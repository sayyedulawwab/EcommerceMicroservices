using Cart.Application.Abstractions.Caching;
using Cart.Application.Abstractions.Clock;
using Cart.Domain.Carts;
using Cart.Infrastructure.Auth;
using Cart.Infrastructure.Caching;
using Cart.Infrastructure.Clock;
using Cart.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();

        AddAuthentication(services);

        services.AddStackExchangeRedisCache(redisOptions =>
        {

            string connection = configuration.GetConnectionString("Redis");

            redisOptions.Configuration = connection;
        });

        services.AddSingleton<ICacheService, CacheService>();
        services.AddScoped<ICartRepository, CartRepository>();

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