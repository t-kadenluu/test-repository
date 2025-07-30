using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UserManagement.API.Models;
using UserManagement.API.Services;
using Xunit;

namespace UserManagement.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _loggerMock = new Mock<ILogger<UserService>>();
        _userService = new UserService(_loggerMock.Object);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreateUser_WhenValidRequest()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "123-456-7890",
            Password = "password123"
        };

        // Act
        var result = await _userService.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be(request.FirstName);
        result.LastName.Should().Be(request.LastName);
        result.Email.Should().Be(request.Email);
        result.PhoneNumber.Should().Be(request.PhoneNumber);
        result.Id.Should().BeGreaterThan(0);
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            PhoneNumber = "098-765-4321",
            Password = "password456"
        };
        var createdUser = await _userService.CreateUserAsync(request);

        // Act
        var result = await _userService.GetUserByIdAsync(createdUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdUser.Id);
        result.FirstName.Should().Be(request.FirstName);
        result.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Act
        var result = await _userService.GetUserByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnTrue_WhenUserExists()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            FirstName = "Bob",
            LastName = "Johnson",
            Email = "bob.johnson@example.com",
            PhoneNumber = "555-123-4567",
            Password = "password789"
        };
        var createdUser = await _userService.CreateUserAsync(request);

        // Act
        var result = await _userService.DeleteUserAsync(createdUser.Id);

        // Assert
        result.Should().BeTrue();
        
        // Verify user is actually deleted
        var deletedUser = await _userService.GetUserByIdAsync(createdUser.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Act
        var result = await _userService.DeleteUserAsync(999);

        // Assert
        result.Should().BeFalse();
    }
}
