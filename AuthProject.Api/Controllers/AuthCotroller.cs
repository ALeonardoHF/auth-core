using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthProject.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [EnableRateLimiting("login")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var deviceInfo = Request.Headers["User-Agent"].ToString();
            var ipAddress  = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result     = await _authService.LoginAsync(request, deviceInfo, ipAddress);
            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var deviceInfo = Request.Headers["User-Agent"].ToString();
            var ipAddress  = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result     = await _authService.RefreshAsync(request.RefreshToken, deviceInfo, ipAddress);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            await _authService.LogoutAsync(request.RefreshToken);
            return NoContent();
        }

        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            await _authService.LogoutAllDevicesAsync(userId);
            return NoContent();
        }
    }
}