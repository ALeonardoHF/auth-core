public interface IEmailService
{
    Task SendConfirmationEmailAsync(string toEmail, string confirmationLink);
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
}
