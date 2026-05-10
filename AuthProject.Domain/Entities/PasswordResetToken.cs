public class PasswordResetToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static PasswordResetToken Create(Guid userId) => new()
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Token = Guid.NewGuid().ToString("N"),
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      IsUsed = false,
      CreatedAt = DateTime.UtcNow  
    };

    public bool isExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsUsed && !isExpired;
    public void MarkAsUsed() => IsUsed = true;
}