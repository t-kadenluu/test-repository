using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Text;
using StackExchange.Redis;
using Microsoft.Extensions.Options;

namespace Caching.Providers
{
    public class DistributedCacheProvider
    {
        private readonly ILogger<DistributedCacheProvider> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly IDatabase _redisDatabase;
        private readonly CacheConfiguration _configuration;

        public DistributedCacheProvider(
            ILogger<DistributedCacheProvider> logger,
            IMemoryCache memoryCache,
            IDistributedCache distributedCache,
            IDatabase redisDatabase,
            IOptions<CacheConfiguration> configuration)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _redisDatabase = redisDatabase;
            _configuration = configuration.Value;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
        {
            _logger.LogDebug("Getting or setting cache entry for key: {Key}", key);

            // Try to get from cache first
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            // Get from factory and cache the result
            var value = await factory();
            if (value != null)
            {
                await SetAsync(key, value, expiration);
            }

            return value;
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            // Try memory cache first for better performance
            if (_memoryCache.TryGetValue(key, out T memoryValue))
            {
                _logger.LogDebug("Cache hit in memory for key: {Key}", key);
                return memoryValue;
            }

            // Try distributed cache
            var distributedValue = await _distributedCache.GetStringAsync(key);
            if (distributedValue != null)
            {
                _logger.LogDebug("Cache hit in distributed cache for key: {Key}", key);
                var deserializedValue = JsonConvert.DeserializeObject<T>(distributedValue);
                
                // Add back to memory cache for faster access
                var memoryExpiration = TimeSpan.FromMinutes(_configuration.MemoryCacheExpirationMinutes);
                _memoryCache.Set(key, deserializedValue, memoryExpiration);
                
                return deserializedValue;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return null;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            var actualExpiration = expiration ?? TimeSpan.FromMinutes(_configuration.DefaultExpirationMinutes);
            
            _logger.LogDebug("Setting cache entry for key: {Key} with expiration: {Expiration}", key, actualExpiration);

            // Set in memory cache
            _memoryCache.Set(key, value, actualExpiration);

            // Set in distributed cache
            var serializedValue = JsonConvert.SerializeObject(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = actualExpiration
            };
            
            await _distributedCache.SetStringAsync(key, serializedValue, options);
        }

        public async Task InvalidatePatternAsync(string pattern)
        {
            _logger.LogInformation("Invalidating cache entries matching pattern: {Pattern}", pattern);
            
            // For Redis, we can use pattern-based deletion
            var server = _redisDatabase.Multiplexer.GetServer(_redisDatabase.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);
            
            foreach (var key in keys)
            {
                await _redisDatabase.KeyDeleteAsync(key);
                _memoryCache.Remove(key.ToString());
            }
        }
    }

    public class CacheConfiguration
    {
        public int DefaultExpirationMinutes { get; set; } = 60;
        public int MemoryCacheExpirationMinutes { get; set; } = 5;
        public string RedisConnectionString { get; set; }
        public bool EnableCompression { get; set; } = true;
    }
}
