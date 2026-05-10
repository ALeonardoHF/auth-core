using Microsoft.Extensions.Configuration;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailConfirmationTokenRepository _confirmationTokenRepository;
    private readonly IEmailService _emailService;
    private readonly string _baseUrl;
    private readonly IAuditLogRepository _auditLogRepository;


    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailConfirmationTokenRepository confirmationTokenRepository,
        IEmailService emailService,
        IConfiguration config,
        IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _confirmationTokenRepository = confirmationTokenRepository;
        _emailService = emailService;
        _baseUrl = config["AppSettings:BaseUrl"]!;
        _auditLogRepository = auditLogRepository;
    }


    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        if (await _userRepository.ExistsWithEmailAsync(request.Email))
            throw new DomainException("Email already in use.");

        var hash = _passwordHasher.Hash(request.Password);
        var user = User.Create(request.Email, hash, request.Role);
        await _userRepository.AddAsync(user);

        var confirmationToken = EmailConfirmationToken.Create(user.Id);
        await _confirmationTokenRepository.AddAsync(confirmationToken);

        var link = $"{_baseUrl}/auth/confirm-email?token={confirmationToken.Token}";

        await _emailService.SendConfirmationEmailAsync(user.Email, link);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditLogEvent.UserRegistered,
            userId: user.Id,
            email: user.Email));

        return ToResponse(user);
    }

    public async Task<UserResponse> GetByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null) throw new NotFoundException("User not found.");
        return ToResponse(user);
    }

    public async Task<IEnumerable<UserResponse>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(ToResponse);
    }

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt);
}
