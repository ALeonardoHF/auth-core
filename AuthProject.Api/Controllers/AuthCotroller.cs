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

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            await _authService.ConfirmEmailAsync(token);
            return Content(EmailTemplates.ConfirmationSuccess(), "text/html");
        }

        [HttpGet("email-preview/confirmation")]
        public IActionResult PreviewConfirmationEmail()
        {
            var fakeLink = "http://localhost:5000/auth/confirm-email?token=PREVIEW";
            var html = EmailTemplates.Confirmation(fakeLink);
            return Content(html, "text/html");
        }

        [HttpGet("email-preview/confirmation-success")]
        public IActionResult PreviewConfirmationSuccess()
        {
            return Content(EmailTemplates.ConfirmationSuccess(), "text/html");
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _authService.ForgotPasswordAsync(request.Email);
            return Ok("Si el email existe recibirás un link para restablecer tu contraseña.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
            return Ok("Contraseña restablecida correctamente.");
        }

        [HttpGet("reset-password")]
        public IActionResult ResetPasswordForm([FromQuery] string token)
        {
            return Content(EmailTemplates.ResetPasswordForm(token), "text/html");
        }

        [HttpPost("reset-password-form")]
        public async Task<IActionResult> ResetPasswordForm([FromForm] string token, [FromForm] string newPassword)
        {
            await _authService.ResetPasswordAsync(token, newPassword);
            return Content(EmailTemplates.PasswordResetSuccess(), "text/html");
        }

    }
}