using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using FluentValidation;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;

namespace IntegrationTests
{
    public class UserManagementIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ILogger<UserManagementIntegrationTest> _logger;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateUserRequest> _validator;

        public UserManagementIntegrationTest(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            
            var serviceProvider = _factory.Services;
            _logger = serviceProvider.GetRequiredService<ILogger<UserManagementIntegrationTest>>();
            _mapper = serviceProvider.GetRequiredService<IMapper>();
            _validator = serviceProvider.GetRequiredService<IValidator<CreateUserRequest>>();
        }

        [Fact]
        public async Task CreateUser_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                Password = "SecurePassword123!"
            };

            var validationResult = await _validator.ValidateAsync(request);
            Assert.True(validationResult.IsValid);

            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Testing user creation for email: {Email}", request.Email);

            // Act
            var response = await _client.PostAsync("/api/users", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var userResponse = System.Text.Json.JsonSerializer.Deserialize<UserResponseDto>(responseString);

            Assert.NotNull(userResponse);
            Assert.Equal(request.Email, userResponse.Email);
            Assert.Equal(request.FirstName, userResponse.FirstName);
            Assert.Equal(request.LastName, userResponse.LastName);

            _logger.LogInformation("User creation test completed successfully for ID: {UserId}", userResponse.Id);
        }

        [Fact]
        public async Task GetUser_ExistingUser_ReturnsUser()
        {
            // Arrange
            var userId = await CreateTestUserAsync();
            _logger.LogInformation("Testing user retrieval for ID: {UserId}", userId);

            // Act
            var response = await _client.GetAsync($"/api/users/{userId}");

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var user = System.Text.Json.JsonSerializer.Deserialize<UserResponseDto>(responseString);

            Assert.NotNull(user);
            Assert.Equal(userId, user.Id);
        }

        [Fact]
        public async Task UpdateUser_ValidRequest_ReturnsUpdatedUser()
        {
            // Arrange
            var userId = await CreateTestUserAsync();
            var updateRequest = new UpdateUserRequest
            {
                FirstName = "Jane",
                LastName = "Smith"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(updateRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Testing user update for ID: {UserId}", userId);

            // Act
            var response = await _client.PutAsync($"/api/users/{userId}", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var updatedUser = System.Text.Json.JsonSerializer.Deserialize<UserResponseDto>(responseString);

            Assert.NotNull(updatedUser);
            Assert.Equal(updateRequest.FirstName, updatedUser.FirstName);
            Assert.Equal(updateRequest.LastName, updatedUser.LastName);
        }

        private async Task<Guid> CreateTestUserAsync()
        {
            var request = new CreateUserRequest
            {
                Email = $"test_{Guid.NewGuid()}@example.com",
                FirstName = "Test",
                LastName = "User",
                Password = "TestPassword123!"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/users", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var user = System.Text.Json.JsonSerializer.Deserialize<UserResponseDto>(responseString);

            return user.Id;
        }
    }

    public class CreateUserRequest
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
    }

    public class UpdateUserRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
