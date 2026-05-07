using System.Security.Claims;

public class TokenVersionMiddleware
{
    private readonly RequestDelegate _next;

    public TokenVersionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        // Si el request no está autenticado, no hay nada que validar
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // Extraer claims del JWT
        var userIdClaim       = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tokenVersionClaim = context.User.FindFirst("token_version")?.Value;

        if (userIdClaim is null || tokenVersionClaim is null)
            throw new UnauthorizedException("Invalid token claims.");

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException("Invalid token claims.");

        if (!int.TryParse(tokenVersionClaim, out var tokenVersion))
            throw new UnauthorizedException("Invalid token claims.");

        // Verificar que el token_version del JWT coincide con el de la DB
        var user = await userRepository.GetByIdAsync(userId);

        if (user is null || !user.IsActive)
            throw new UnauthorizedException("User not found or inactive.");

        if (user.TokenVersion != tokenVersion)
            throw new UnauthorizedException("Token has been revoked.");

        await _next(context);
    }
}
