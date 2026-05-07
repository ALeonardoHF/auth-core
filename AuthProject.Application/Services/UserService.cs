public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        if (await _userRepository.ExistsWithEmailAsync(request.Email))
            throw new DomainException("Email already in use.");

        var hash = _passwordHasher.Hash(request.Password);
        var user = User.Create(request.Email, hash, request.Role);
        await _userRepository.AddAsync(user);

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
