using Microsoft.Extensions.Options;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly IAuditLogRepository _auditLogRepository;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings,
        IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request, string? deviceInfo, string? ipAddress)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        // verificar si esta bloqueado el usuario
        if(user?.IsLocked() == true)
        {
            await _auditLogRepository.AddAsync(AuditLog.Create(
                AuditLogEvent.AccountLocked,
                userId: user.Id,
                email: user.Email,
                ipAddress: ipAddress,
                deviceInfo: deviceInfo,
                details: $"Bloqueado hasta {user.LockedUntil}"));
            
            throw new UnauthorizedException($"Cuenta bloqueada hasta {user.LockedUntil}");
        }

        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            if(user != null && !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                await _auditLogRepository.AddAsync(AuditLog.Create(
                AuditLogEvent.LoginFailed,
                userId: user.Id,
                email: user.Email,
                ipAddress: ipAddress,
                deviceInfo: deviceInfo,
                details: $"Intento {user.FailedLoginAttempts + 1, 5} de 5"));
                user.RegisterFailedLogin();
                await _userRepository.UpdateAsync(user);
            }
                
            throw new UnauthorizedException("Invalid credentials.");
        }
            

        user.RecordLogin();
        // actualizar los intentos a 0 y la fecha de locked a null
        user.ResetLoginAttempts();
        await _userRepository.UpdateAsync(user);
        await _auditLogRepository.AddAsync(AuditLog.Create(
                AuditLogEvent.LoginSuccess,
                userId: user.Id,
                email: user.Email,
                ipAddress: ipAddress,
                deviceInfo: deviceInfo));

        var jwt = _tokenService.GenerateJwt(user);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();

        var refreshToken = RefreshToken.Create(
            user.Id,
            rawRefreshToken,
            _jwtSettings.RefreshTokenExpirationDays,
            deviceInfo,
            ipAddress);

        await _refreshTokenRepository.AddAsync(refreshToken);

        

        return new LoginResponse(jwt, rawRefreshToken, user.Role.ToString());
    }

    public async Task<LoginResponse> RefreshAsync(
        string refreshToken, string? deviceInfo, string? ipAddress)
    {
        var existing = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

        if (existing is null || !existing.IsValid)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var user = await _userRepository.GetByIdAsync(existing.UserId);

        if (user is null || !user.IsActive)
            throw new UnauthorizedException("User not found or inactive.");

        existing.Revoke();
        await _refreshTokenRepository.UpdateAsync(existing);

        var jwt = _tokenService.GenerateJwt(user);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();

        var newRefreshToken = RefreshToken.Create(
            user.Id,
            rawRefreshToken,
            _jwtSettings.RefreshTokenExpirationDays,
            deviceInfo,
            ipAddress);

        await _refreshTokenRepository.AddAsync(newRefreshToken);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditLogEvent.TokenRefreshed,
            userId: user.Id,
            email: user.Email,
            ipAddress: ipAddress,
            deviceInfo: deviceInfo));

        return new LoginResponse(jwt, rawRefreshToken, user.Role.ToString());
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var existing = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

        if (existing is null || existing.IsRevoked)
            return;

        existing.Revoke();
        await _refreshTokenRepository.UpdateAsync(existing);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditLogEvent.Logout,
            userId: existing.UserId,
            ipAddress: null,
            deviceInfo: null));

    }

    public async Task LogoutAllDevicesAsync(Guid userId)
    {
        await _refreshTokenRepository.RevokeAllByUserIdAsync(userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null) return;

        user.IncrementTokenVersion();
        await _userRepository.UpdateAsync(user);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditLogEvent.LogoutAllDevices,
            userId: userId));

    }
}
