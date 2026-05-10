using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Configuration;


namespace AuthProject.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IEmailConfirmationTokenRepository> _confirmationTokenRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly UserService _sut;


    public UserServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                { "AppSettings:BaseUrl", "http://localhost:5000" }
            })
            .Build();

        _sut = new UserService(
            _userRepo.Object,
            _hasher.Object,
            _confirmationTokenRepo.Object,
            _emailService.Object,
            config);
    }


    [Fact]
    public async Task CreateAsync_EmailDuplicado_LanzaDomainException()
    {
        _userRepo.Setup(r => r.ExistsWithEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(new CreateUserRequest("leo@test.com", "pass", Role.Client));

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task CreateAsync_EmailNuevo_RetornaUserResponse()
    {
        _userRepo.Setup(r => r.ExistsWithEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _hasher.Setup(h => h.Hash(It.IsAny<string>()))
            .Returns("hash-falso");

        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(new CreateUserRequest("leo@test.com", "pass123", Role.Client));

        _confirmationTokenRepo.Setup(r => r.AddAsync(It.IsAny<EmailConfirmationToken>()))
            .Returns(Task.CompletedTask);

        _emailService.Setup(s => s.SendConfirmationEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        result.Email.Should().Be("leo@test.com");
        result.Role.Should().Be("Client");
    }

    [Fact]
    public async Task GetById_UsuarioExistente_RetornaUserResponse()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(User.Create("leo@test.com", "hash", Role.Client));

        var result = await _sut.GetByIdAsync(It.IsAny<Guid>());
    }

    [Fact]
    public async Task GetById_UsuarioInexistente_LanzaNotFoundException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        var act = () => _sut.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}