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
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.LoginAsync(request, deviceInfo, ipAddress);
            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var deviceInfo = Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.RefreshAsync(request.RefreshToken, deviceInfo, ipAddress);
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

        [Authorize]
        [HttpPost("2fa/setup")]
        public async Task<IActionResult> SetupTwoFactor()
        {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var result = await _authService.SetupTwoFactorAsync(userId);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("2fa/enable")]
        public async Task<IActionResult> EnableTwoFactor([FromBody] VerifyTwoFactorCodeRequest request)
        {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            await _authService.EnableTwoFactorAsync(userId, request.Code);
            return Ok("2FA activado correctamente.");
        }


        [HttpPost("2fa/verify")]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
        {
            var deviceInfo = Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.VerifyTwoFactorAsync(request.Email, request.Code, deviceInfo, ipAddress);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("2fa/setup-page")]
        public async Task<IActionResult> SetupTwoFactorPage()
        {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var result = await _authService.SetupTwoFactorAsync(userId);

            var html = $"""
                <!DOCTYPE html>
                <html>
                <head><meta charset="utf-8"></head>
                <body style="margin:0;padding:0;background-color:#1A0A2E;font-family:Arial,sans-serif;">
                <table width="100%" cellpadding="0" cellspacing="0">
                    <tr>
                    <td align="center" style="padding:40px 20px;">
                        <table width="520" cellpadding="0" cellspacing="0" style="background-color:#0D0D0D;border-radius:16px;overflow:hidden;border:1px solid #6B3FA0;">
                        <tr>
                            <td style="background:linear-gradient(135deg,#1A0A2E,#6B3FA0);padding:40px;text-align:center;">
                            <div style="font-size:48px;">👻</div>
                            <h1 style="color:#C9A8E0;font-size:26px;margin:12px 0 0;">AuthCore — 2FA Setup</h1>
                            </td>
                        </tr>
                        <tr>
                            <td style="padding:40px;text-align:center;">
                            <p style="color:#F2C4B8;margin:0 0 20px;">Escanea este QR con Google Authenticator</p>
                            <img src="data:image/png;base64,{result.QrCodeBase64}" style="border-radius:8px;" />
                            <p style="color:#9B6DC5;font-size:12px;margin:20px 0 0;">Código manual: <strong style="color:#C9A8E0;">{result.ManualCode}</strong></p>
                            </td>
                        </tr>
                        </table>
                    </td>
                    </tr>
                </table>
                </body>
                </html>
                """;

            return Content(html, "text/html");
        }

        [Authorize]
        [HttpPost("2fa/disable")]
        public async Task<IActionResult> DisableTwoFactor([FromBody] VerifyTwoFactorCodeRequest request)
        {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            await _authService.DisableTwoFactorAsync(userId, request.Code);
            return Ok("2FA desactivado correctamente.");
        }

        [HttpPost("2fa/recovery/request")]
        public async Task<IActionResult> RequestTwoFactorRecovery([FromBody] ForgotPasswordRequest request)
        {
            await _authService.RequestTwoFactorRecoveryAsync(request.Email);
            return Ok("Si el email existe recibirás un link para recuperar el acceso.");
        }

        [HttpPost("2fa/recovery/confirm")]
        public async Task<IActionResult> ConfirmTwiFactorRecovery([FromBody] TwoFactorRecoveryConfirmRequest request)
        {
            await _authService.ConfirmTwoFactorRecoveryAsync(request.Token, request.Password);
            return Ok("2FA desactivado. Ya puedes iniciar sesión normalmente.");
        }

        [HttpGet("2fa/recovery/confirm")]
        public IActionResult TwoFactorRecoveryForm([FromQuery] string token)
        {
            return Content(EmailTemplates.TwoFactorRecoveryForm(token), "text/html");
        }

        [HttpPost("2fa/recovery/confirm-form")]
        public async Task<IActionResult> TwoFactorRecoveryConfirmForm([FromForm] string token, [FromForm] string password)
        {
            await _authService.ConfirmTwoFactorRecoveryAsync(token, password);
            return Content(EmailTemplates.TwoFactorRecoverySuccess(), "text/html");
        }

    }
}