public interface ITokenService
{
    string GenerateJwt(User user);
    string GenerateRefreshToken();
}