// Test utilities and helpers
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace TestUtilities;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the app's database context registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Override other services for testing
            services.AddScoped<IEmailService, MockEmailService>();
            services.AddScoped<IPaymentService, MockPaymentService>();
        });

        builder.UseEnvironment("Testing");
    }
}

public class MockEmailService : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body)
    {
        // Mock implementation - don't actually send emails in tests
        return Task.CompletedTask;
    }

    public Task SendTemplateEmailAsync(string to, string templateId, object templateData)
    {
        // Mock implementation
        return Task.CompletedTask;
    }
}

public class MockPaymentService : IPaymentService
{
    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        // Mock implementation - simulate successful payment
        return Task.FromResult(new PaymentResult
        {
            IsSuccessful = true,
            TransactionId = Guid.NewGuid().ToString(),
            ProcessedAt = DateTime.UtcNow
        });
    }

    public Task<RefundResult> ProcessRefundAsync(RefundRequest request)
    {
        // Mock implementation
        return Task.FromResult(new RefundResult
        {
            IsSuccessful = true,
            RefundId = Guid.NewGuid().ToString(),
            ProcessedAt = DateTime.UtcNow
        });
    }
}

public static class TestDataSeeder
{
    public static void SeedTestData(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Clear existing data
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        // Seed test users
        var testUsers = new[]
        {
            new User { FirstName = "Test", LastName = "User1", Email = "test1@example.com", IsActive = true },
            new User { FirstName = "Test", LastName = "User2", Email = "test2@example.com", IsActive = true },
            new User { FirstName = "Test", LastName = "User3", Email = "test3@example.com", IsActive = false }
        };

        context.Users.AddRange(testUsers);
        context.SaveChanges();
    }
}

public static class HttpClientExtensions
{
    public static async Task<T?> GetFromJsonAsync<T>(this HttpClient client, string requestUri)
    {
        var response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, string requestUri, T data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(requestUri, content);
    }

    public static async Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, string requestUri, T data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PutAsync(requestUri, content);
    }
}

public class TestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }
    
    public TestFixture()
    {
        var services = new ServiceCollection();
        
        // Add test services
        services.AddLogging(builder => builder.AddConsole());
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        
        ServiceProvider = services.BuildServiceProvider();
        
        // Seed test data
        TestDataSeeder.SeedTestData(ServiceProvider);
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
