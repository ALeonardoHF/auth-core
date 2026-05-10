public class EmailConfirmationToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private EmailConfirmationToken() { }

    public static EmailConfirmationToken Create(Guid userId)
    {
        return new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired;

    public void MarkAsUsed() => IsUsed = true;
}
