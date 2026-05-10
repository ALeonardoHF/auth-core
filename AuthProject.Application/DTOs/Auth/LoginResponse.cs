public record AuthResponse(string AccessToken, string RefreshToken, string Role);
public record LoginResponse(bool RequiresTwoFactor, string? Email, AuthResponse? Auth);
