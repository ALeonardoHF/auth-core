public interface IEmailConfirmationTokenRepository
{
    Task AddAsync(EmailConfirmationToken token);
    Task<EmailConfirmationToken?> GetByTokenAsync(string token);
    Task UpdateAsync(EmailConfirmationToken token);
}
