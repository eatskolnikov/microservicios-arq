using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ProductService.Infrastructure.Caching;

public static class RedisDistributedCacheExtensions
{
    public static IServiceCollection AddRedisDistributedCache(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(connectionString));
        
        services.AddSingleton<IDistributedCache, RedisCacheService>();

        return services;
    }
}

