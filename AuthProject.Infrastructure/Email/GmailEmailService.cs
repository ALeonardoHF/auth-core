using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

public class GmailEmailService : IEmailService
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _user;
    private readonly string _pass;
    private readonly string _from;

    public GmailEmailService(IConfiguration config)
    {
        _host = config["Smtp:Host"]!;
        _port = int.Parse(config["Smtp:Port"]!);
        _user = config["Smtp:User"]!;
        _pass = config["Smtp:Pass"]!;
        _from = config["Smtp:From"]!;
    }

    public async Task SendConfirmationEmailAsync(string toEmail, string confirmationLink)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Confirma tu cuenta";
        message.Body = new TextPart("html") { Text = EmailTemplates.Confirmation(confirmationLink)};

        await SendAsync(message);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Restablecer contraseña";
        message.Body = new TextPart("html") { Text = EmailTemplates.PasswordReset(resetLink)};

        await SendAsync(message);
    }

    private async Task SendAsync(MimeMessage message)
    {
        using var smtp = new SmtpClient();
        var socketOptions = _port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await smtp.ConnectAsync(_host, _port, socketOptions);
        await smtp.AuthenticateAsync(_user, _pass);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }

    public async Task SendTwoFactorRecoveryEmailAsync(string toEmail, string recoveryLink)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Recuperación de autenticación de dos factores";
        message.Body = new TextPart("html") { Text = EmailTemplates.TwoFactorRecovery(recoveryLink) };

        await SendAsync(message);
    }

}
