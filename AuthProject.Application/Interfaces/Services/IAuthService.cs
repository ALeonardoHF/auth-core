public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? deviceInfo, string? ipAddress);
    Task<LoginResponse> RefreshAsync(string refreshToken, string? deviceInfo, string? ipAddress);
    Task LogoutAsync(string refreshToken);
    Task LogoutAllDevicesAsync(Guid userId);
    Task ConfirmEmailAsync(string token);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(string token, string newPassword);
}