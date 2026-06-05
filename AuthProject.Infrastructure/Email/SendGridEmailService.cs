using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;

public class SendGridEmailService : IEmailService
{
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailService(IConfiguration config)
    {
        _apiKey    = config["SendGrid:ApiKey"]!;
        _fromEmail = config["SendGrid:FromEmail"]!;
        _fromName  = config["SendGrid:FromName"]!;
    }

    public async Task SendConfirmationEmailAsync(string toEmail, string confirmationLink)
    {
        var client  = new SendGridClient(_apiKey);
        var from    = new EmailAddress(_fromEmail, _fromName);
        var to      = new EmailAddress(toEmail);
        var subject = "Confirma tu cuenta";
        var body    = $"Haz click en el siguiente enlace para confirmar tu cuenta: <a href='{confirmationLink}'>Confirmar cuenta</a>";
        var msg     = MailHelper.CreateSingleEmail(from, to, subject, body, body);

        var response = await client.SendEmailAsync(msg);

        if ((int)response.StatusCode >= 400)
        {
            var body2 = await response.Body.ReadAsStringAsync();
            throw new Exception($"SendGrid error {response.StatusCode}: {body2}");
        }
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        var client  = new SendGridClient(_apiKey);
        var from    = new EmailAddress(_fromEmail, _fromName);
        var to      = new EmailAddress(toEmail);
        var html    = EmailTemplates.PasswordReset(resetLink);
        var msg     = MailHelper.CreateSingleEmail(from, to, "Restablecer contraseña", html, html);

        var response = await client.SendEmailAsync(msg);
        if ((int)response.StatusCode >= 400)
        {
            var body = await response.Body.ReadAsStringAsync();
            throw new Exception($"SendGrid error {response.StatusCode}: {body}");
        }
    }

    public Task SendTwoFactorRecoveryEmailAsync(string toEmail, string recoveryLink)
    => throw new NotImplementedException();

}
