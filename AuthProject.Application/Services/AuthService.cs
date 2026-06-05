using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IEmailConfirmationTokenRepository _confirmationTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IEmailService _emailService;
    private readonly string _baseUrl;
    private readonly ITotpService _totpService;


    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings,
        IEmailConfirmationTokenRepository confirmationTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IAuditLogRepository auditLogRepository,
        IEmailService emailService,
        IConfiguration config,
        ITotpService totpService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
        _auditLogRepository = auditLogRepository;
        _confirmationTokenRepository = confirmationTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _emailService = emailService;
        _baseUrl = config["AppSettings:BaseUrl"]!;
        _totpService = totpService;
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
        
        if (!user.IsEmailConfirmed)
                throw new UnauthorizedException("Debes confirmar tu email antes de iniciar sesión.");

        user.RecordLogin();
        // actualizar los intentos a 0 y la fecha de locked a null
        user.ResetLoginAttempts();
        await _userRepository.UpdateAsync(user);
        if (user.IsTwoFactorEnabled)
            return new LoginResponse(true, user.Email, null);

        var jwt = _tokenService.GenerateJwt(user);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();

        var refreshToken = RefreshToken.Create(
            user.Id,
            rawRefreshToken,
            _jwtSettings.RefreshTokenExpirationDays,
            deviceInfo,
            ipAddress);

        await _refreshTokenRepository.AddAsync(refreshToken);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditLogEvent.LoginSuccess,
            userId: user.Id,
            email: user.Email,
            ipAddress: ipAddress,
            deviceInfo: deviceInfo));

        return new LoginResponse(false, null, new AuthResponse(jwt, rawRefreshToken, user.Role.ToString()));

    }

    public async Task<AuthResponse> RefreshAsync(
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

        return new AuthResponse(jwt, rawRefreshToken, user.Role.ToString());
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

    public async Task ConfirmEmailAsync(string token)
    {
        var confirmationToken = await _confirmationTokenRepository.GetByTokenAsync(token);

        if (confirmationToken is null || !confirmationToken.IsValid)
            throw new UnauthorizedException("Token inválido o expirado.");

        var user = await _userRepository.GetByIdAsync(confirmationToken.UserId);
        if (user is null) throw new NotFoundException("Usuario no encontrado.");

        user.ConfirmEmail();
        confirmationToken.MarkAsUsed();

        await _userRepository.UpdateAsync(user);
        await _confirmationTokenRepository.UpdateAsync(confirmationToken);
        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditLogEvent.EmailConfirmed,
            userId: user.Id,
            email: user.Email));

    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null || !user.IsActive) return;

        var token = PasswordResetToken.Create(user.Id);
        await _passwordResetTokenRepository.AddAsync(token);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditLogEvent.PasswordResetRequested,
            userId: user.Id,
            email: user.Email));

        var link = $"{_baseUrl}/auth/reset-password?token={token.Token}";
        await _emailService.SendPasswordResetEmailAsync(user.Email, link);
    }

    public async Task ResetPasswordAsync(string token, string newPassword)
    {
        var resetToken = await _passwordResetTokenRepository.GetByTokenAsync(token);
        if (resetToken is null || !resetToken.IsValid)
            throw new UnauthorizedException("Token inválido o expirado.");

        var user = await _userRepository.GetByIdAsync(resetToken.UserId);
        if (user is null) throw new NotFoundException("Usuario no encontrado.");

        var newHash = _passwordHasher.Hash(newPassword);
        user.ChangePassword(newHash);

        resetToken.MarkAsUsed();

        await _userRepository.UpdateAsync(user);
        await _passwordResetTokenRepository.UpdateAsync(resetToken);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditLogEvent.PasswordResetCompleted,
            userId: user.Id,
            email: user.Email));

    }

    public async Task<TwoFactorSetupResponse> SetupTwoFactorAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null) throw new NotFoundException("Usuario no encontrado.");

        var secret = _totpService.GenerateSecret();
        var qrBase64 = _totpService.GenerateQrCodeBase64(user.Email, secret);

        user.EnableTwoFactor(secret);
        await _userRepository.UpdateAsync(user);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditLogEvent.TwoFactorSetup,
            userId: user.Id,
            email: user.Email));

        return new TwoFactorSetupResponse(qrBase64, secret);
    }

    public async Task<AuthResponse> VerifyTwoFactorAsync(string email, string code, string? deviceInfo, string? ipAddress)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null || !user.IsTwoFactorEnabled)
            throw new UnauthorizedException("2FA no configurado.");

        if (!_totpService.Verify(user.TotpSecret!, code))
        {
            await _auditLogRepository.AddAsync(AuditLog.Create(
                AuditLogEvent.TwoFactorFailed,
                userId: user.Id,
                email: user.Email,
                ipAddress: ipAddress,
                deviceInfo: deviceInfo));
            throw new UnauthorizedException("Código inválido.");
        }

        var jwt = _tokenService.GenerateJwt(user);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var token = RefreshToken.Create(user.Id, rawRefreshToken, _jwtSettings.RefreshTokenExpirationDays, deviceInfo, ipAddress);
        await _refreshTokenRepository.AddAsync(token);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditLogEvent.TwoFactorSuccess,
            userId: user.Id,
            email: user.Email,
            ipAddress: ipAddress,
            deviceInfo: deviceInfo));

        return new AuthResponse(jwt, rawRefreshToken, user.Role.ToString());
    }

    public async Task DisableTwoFactorAsync(Guid userId, string code)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null) throw new NotFoundException("Usuario no encontrado");

        if (!user.IsTwoFactorEnabled)
            throw new DomainException("El 2FA no está activado.");
        
        if (!_totpService.Verify(user.TotpSecret!, code))
        {
            await _auditLogRepository.AddAsync(AuditLog.Create(
                AuditLogEvent.TwoFactorFailed,
                userId: user.Id,
                email: user.Email));
            
            throw new UnauthorizedAccessException("Código inválido.");
        }

        user.DisableTwoFactor();
        await _userRepository.UpdateAsync(user);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditLogEvent.TwoFactorDisabled,
            userId: user.Id,
            email: user.Email));
    }

    public async Task RequestTwoFactorRecoveryAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null || !user.IsActive) return;

        var token = PasswordResetToken.Create(user.Id);
        await _passwordResetTokenRepository.AddAsync(token);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditLogEvent.TwoFactorRecoveryRequested,
            userId: user.Id,
            email: user.Email));

        var link = $"{_baseUrl}/auth/2fa/recovery/confirm?token={token.Token}";
        await _emailService.SendTwoFactorRecoveryEmailAsync(user.Email, link);
    }

    public async Task ConfirmTwoFactorRecoveryAsync(string token, string password)
    {
        var resetToken = await _passwordResetTokenRepository.GetByTokenAsync(token);
        if (resetToken is null || !resetToken.IsValid)
            throw new UnauthorizedAccessException("Token inválido o expirado");

        var user = await _userRepository.GetByIdAsync(resetToken.UserId);
        if (user is null) throw new NotFoundException("Contraseña incorrecta.");

        if (!_passwordHasher.Verify(password, user.PasswordHash))
            throw new UnauthorizedException("Contraseña incorrecta.");

        user.DisableTwoFactor();
        resetToken.MarkAsUsed();

        await _userRepository.UpdateAsync(user);
        await _passwordResetTokenRepository.UpdateAsync(resetToken);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditLogEvent.TwoFactorRecoveryCompleted,
            userId: user.Id,
            email: user.Email));
    }


}
