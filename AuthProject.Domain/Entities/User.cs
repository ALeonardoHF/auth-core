public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public Role Role { get; private set; }
    public bool IsActive { get; private set; }
    public int TokenVersion { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastLogin { get; private set; }
    public bool IsDeleted { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public bool IsEmailConfirmed { get; private set; }


    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() {}

    public static User Create(string email, string passwordHash, Role role)
    {
        if(string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");
        if(string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        return new User
        {
          Id = Guid.NewGuid(),
          Email = email.ToLowerInvariant().Trim(),
          PasswordHash = passwordHash,
          Role = role,
          IsActive = true,
          TokenVersion = 1,
          CreatedAt = DateTime.UtcNow,
          IsDeleted = false,
          FailedLoginAttempts = 0,
          LockedUntil = null,
          IsEmailConfirmed = false
        };
    }

    public void RecordLogin() => LastLogin = DateTime.UtcNow;

    public void IncrementTokenVersion()
    {
        TokenVersion++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RegisterFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
            LockedUntil = DateTime.UtcNow.AddMinutes(15);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetLoginAttempts()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsLocked() => LockedUntil > DateTime.UtcNow;

    public void ConfirmEmail() => IsEmailConfirmed = true;

    public void ChangePassword(string newHash) => PasswordHash = newHash;

}