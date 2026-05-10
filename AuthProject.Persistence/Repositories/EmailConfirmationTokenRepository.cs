using Microsoft.EntityFrameworkCore;

public class EmailConfirmationTokenRepository : IEmailConfirmationTokenRepository
{
    private readonly AppDbContext _context;

    public EmailConfirmationTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EmailConfirmationToken token)
    {
        await _context.EmailConfirmationTokens.AddAsync(token);
        await _context.SaveChangesAsync();
    }

    public async Task<EmailConfirmationToken?> GetByTokenAsync(string token)
    {
        return await _context.EmailConfirmationTokens
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    public async Task UpdateAsync(EmailConfirmationToken token)
    {
        _context.EmailConfirmationTokens.Update(token);
        await _context.SaveChangesAsync();
    }
}
