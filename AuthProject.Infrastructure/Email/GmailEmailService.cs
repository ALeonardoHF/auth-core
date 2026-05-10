using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

public class GmailEmailService : IEmailService
{
    private readonly string _from;
    private readonly string _appPassword;

    public GmailEmailService(IConfiguration config)
    {
        _from        = config["Gmail:Email"]!;
        _appPassword = config["Gmail:AppPassword"]!;
    }

    public async Task SendConfirmationEmailAsync(string toEmail, string confirmationLink)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Confirma tu cuenta";
        message.Body = new TextPart("html") { Text = EmailTemplates.Confirmation(confirmationLink) };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_from, _appPassword);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Restablecer contraseña";
        message.Body = new TextPart("html") { Text = EmailTemplates.PasswordReset(resetLink) };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_from, _appPassword);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }

}
