public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? deviceInfo, string? ipAddress);
    Task<AuthResponse> RefreshAsync(string refreshToken, string? deviceInfo, string? ipAddress);
    Task LogoutAsync(string refreshToken);
    Task LogoutAllDevicesAsync(Guid userId);
    Task ConfirmEmailAsync(string token);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(string token, string newPassword);
    Task<TwoFactorSetupResponse> SetupTwoFactorAsync(Guid userId);
    Task EnableTwoFactorAsync(Guid userId, string code);
    Task<AuthResponse> VerifyTwoFactorAsync(string email, string code, string? deviceInfo, string? ipAddress);
    Task DisableTwoFactorAsync(Guid userId, string code);
    Task RequestTwoFactorRecoveryAsync(string email);
    Task ConfirmTwoFactorRecoveryAsync(string token, string password);

}