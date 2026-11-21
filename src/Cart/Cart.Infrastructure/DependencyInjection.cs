using Cart.Application.Abstractions.Caching;
using Cart.Domain.Carts;
using Cart.Infrastructure.Auth;
using Cart.Infrastructure.Caching;
using Cart.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.DomainEvents;

namespace Cart.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // .NET's own clock abstraction; tests replace it with a fake time provider.
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

        AddAuthentication(services);

        services.AddStackExchangeRedisCache(redisOptions =>
        {
            string connection = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Redis' is not configured.");

            redisOptions.Configuration = connection;
        });

        services.AddSingleton<ICacheService, CacheService>();
        services.AddScoped<ICartRepository, CartRepository>();

        services.AddHealthChecks()
            .AddCheck<CacheHealthCheck>("redis");

        return services;
    }

    private static void AddAuthentication(IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.ConfigureOptions<JwtOptionsSetup>();

        services.ConfigureOptions<JwtBearerOptionsSetup>();
    }

}