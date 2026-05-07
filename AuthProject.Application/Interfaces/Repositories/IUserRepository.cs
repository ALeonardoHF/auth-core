public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsWithEmailAsync(string email);
    Task AddAsync (User user);
    Task UpdateAsync(User user);
    Task<IEnumerable<User>> GetAllAsync();
}