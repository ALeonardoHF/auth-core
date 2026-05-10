public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token);
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task UpdateAsync(PasswordResetToken token);
}