using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReliableEvents.Sample.Application.Abstractions;
using ReliableEvents.Sample.Infrastructure.Idempotency;
using ReliableEvents.Sample.Infrastructure.Messaging;
using StackExchange.Redis;

namespace ReliableEvents.Sample.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        var redisConnectionString = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()?.ConnectionString
                                    ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddScoped<IEventPublisher, RabbitMqPublisher>();
        services.AddScoped<IIdempotencyStore, RedisIdempotencyStore>();

        return services;
    }
}
