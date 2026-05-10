using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace AuthProject.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepo = new();
    private readonly Mock<IEmailConfirmationTokenRepository> _confirmationTokenRepo = new();
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepo = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IConfiguration> _configMock = new();

    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var settings = Options.Create(new JwtSettings { RefreshTokenExpirationDays = 7 });
        _auditLogRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _configMock.Setup(c => c["AppSettings:BaseUrl"]).Returns("http://localhost:5000");
        _sut = new AuthService(
            _userRepo.Object,
            _refreshTokenRepo.Object,
            _hasher.Object,
            _tokenService.Object,
            settings,
            _confirmationTokenRepo.Object,
            _passwordResetTokenRepo.Object,
            _auditLogRepo.Object,
            _emailServiceMock.Object,
            _configMock.Object);
    }

    [Fact]
    public async Task Login_ConEmailInexistente()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var act = () => _sut.LoginAsync(new LoginRequest("nadie@test.com", "pass"), null, null);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_PasswordIncorrecto()
    {
        var user = User.Create("leo@test.com", "hash", Role.Client);

        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), user.PasswordHash))
            .Returns(false);
        
        var act = () => _sut.LoginAsync(new LoginRequest("leo@test.com", "pass"), null, null);
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_UsuarioDesactivado()
    {
        var user = User.Create("leo@test.com", "hash", Role.Client);
        user.Deactivate();

        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        // Opcional ya que no entra, verifica primero si el usuario esta activo o no.
       _hasher.Setup(h => h.Verify(It.IsAny<string>(), user.PasswordHash))
            .Returns(true);

        var act = () => _sut.LoginAsync(new LoginRequest("leo@test.com", "hash"), null, null);
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_Correcto()
    {
        var user = User.Create("leo@test.com", "hash", Role.Client);
        user.ConfirmEmail();

        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

       _hasher.Setup(h => h.Verify(It.IsAny<string>(), user.PasswordHash))
            .Returns(true);

        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        
        _refreshTokenRepo.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
        
        _tokenService.Setup(t => t.GenerateJwt(user))
            .Returns("jwt-falso");

        _tokenService.Setup(r => r.GenerateRefreshToken())
            .Returns("refresh-falso");

        var result = await _sut.LoginAsync(new LoginRequest("leo@test.com", "hash"), null, null);
        
        result.AccessToken.Should().Be("jwt-falso");
        result.RefreshToken.Should().Be("refresh-falso");
        result.Role.Should().Be("Client");
    }

    [Fact]
    public async Task RefreshToken_Correcto()
    {
        var user = User.Create("leo@test.com", "hash", Role.Client);

        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("token-viejo"))
            .ReturnsAsync(RefreshToken.Create(user.Id, "token-viejo", expirationDays: 7));
        
        _userRepo.Setup(r => r.GetByIdAsync(user.Id))
         .ReturnsAsync(user);

        _tokenService.Setup(t => t.GenerateJwt(user))
            .Returns("jwt-falso");

        _tokenService.Setup(r => r.GenerateRefreshToken())
            .Returns("refresh-falso");

        var result = await _sut.RefreshAsync("token-viejo", null, null);
        
        result.AccessToken.Should().Be("jwt-falso");
        result.RefreshToken.Should().Be("refresh-falso");
        result.Role.Should().Be("Client");
    }

    [Fact]
    public async Task RefreshToken_Revocado()
    {
        var user = User.Create("leo@test.com", "hash", Role.Client);
        
        var token = RefreshToken.Create(user.Id, "token-revocado", expirationDays: 7);
        
        token.Revoke();
        
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("token-revocado"))
            .ReturnsAsync(token);

        var act = () => _sut.RefreshAsync("token-revocado", null, null);
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task RefreshToken_Inexistente()
    {
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync(It.IsAny<string>()))
                     .ReturnsAsync((RefreshToken?)null);

        var act = () => _sut.RefreshAsync("token-inexistente", null, null);
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task RefreshToken_Expirado()
    {
        var user = User.Create("leo@test.com", "hash", Role.Client);
        
        var token = RefreshToken.Create(user.Id, "token-expirado", expirationDays: -1);
        
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("token-expirado"))
            .ReturnsAsync(token);

        var act = () => _sut.RefreshAsync("token-expirado", null, null);
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Logout_TokenValido_RevocaElToken()
    {
        var user = User.Create("leo@test.com", "hash", Role.Client);
        var token = RefreshToken.Create(user.Id, "token-valido", expirationDays: 7);

        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("token-valido"))
                        .ReturnsAsync(token);
        _refreshTokenRepo.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>()))
                        .Returns(Task.CompletedTask);

        await _sut.LogoutAsync("token-valido");

        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Logout_TokenInexistente_NoLanzaExcepcion()
    {
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync(It.IsAny<string>()))
                        .ReturnsAsync((RefreshToken?)null);

        var act = () => _sut.LogoutAsync("token-fantasma");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Logout_TokenYaRevocado_NoLanzaExcepcion()
    {
        var user = User.Create("leo@test.com", "hash", Role.Client);
        var token = RefreshToken.Create(user.Id, "token-revocado", expirationDays: 7);
        token.Revoke();

        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("token-revocado"))
                        .ReturnsAsync(token);

        var act = () => _sut.LogoutAsync("token-revocado");

        await act.Should().NotThrowAsync();
    }
}
