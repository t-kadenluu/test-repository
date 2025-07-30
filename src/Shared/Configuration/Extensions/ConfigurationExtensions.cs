// Configuration extensions and helpers
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Configuration.Extensions;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddApplicationConfiguration(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Add configuration-related services
        return services;
    }
    
    public static T GetRequiredConfiguration<T>(this IConfiguration configuration, string key)
        where T : class, new()
    {
        var section = configuration.GetSection(key);
        var config = section.Get<T>() ?? new T();
        return config;
    }
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = 30;
    public bool EnableSensitiveDataLogging { get; set; } = false;
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationInMinutes { get; set; } = 60;
}

public class RedisSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public int Database { get; set; } = 0;
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);
}
