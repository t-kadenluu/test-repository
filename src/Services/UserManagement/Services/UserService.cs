using UserManagement.API.Models;

namespace UserManagement.API.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<bool> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(int id);
}

public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;
    private static readonly List<User> _users = new();
    private static int _nextId = 1;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        await Task.Delay(10); // Simulate async operation
        return _users.Select(MapToDto);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        await Task.Delay(10); // Simulate async operation
        var user = _users.FirstOrDefault(u => u.Id == id);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        await Task.Delay(10); // Simulate async operation
        
        var user = new User
        {
            Id = _nextId++,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _users.Add(user);
        _logger.LogInformation("Created user with id {UserId}", user.Id);
        
        return MapToDto(user);
    }

    public async Task<bool> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        await Task.Delay(10); // Simulate async operation
        
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return false;
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Updated user with id {UserId}", id);
        return true;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        await Task.Delay(10); // Simulate async operation
        
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return false;
        }

        _users.Remove(user);
        _logger.LogInformation("Deleted user with id {UserId}", id);
        return true;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            IsActive = user.IsActive
        };
    }

    private static string HashPassword(string password)
    {
        // This is a simplified hash - in real applications use BCrypt or similar
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "salt"));
    }
}
