using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Application.Abstractions.Email;
using Identity.Application.Abstractions.Data;
using Identity.Infrastructure.Auth;
using Identity.Infrastructure.BackgroundJobs;
using Identity.Infrastructure.Clock;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.DomainEvents;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();
        services.AddSingleton(TimeProvider.System);

        services.Configure<AccountOptions>(configuration.GetSection(AccountOptions.SectionName));
        services.Configure<AdminUserOptions>(configuration.GetSection(AdminUserOptions.SectionName));
        services.Configure<TokenCleanupOptions>(configuration.GetSection(TokenCleanupOptions.SectionName));

        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddHostedService<TokenCleanupService>();

        AddPersistence(services, configuration);
        AddAuthentication(services);

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("EcommerceDB") ?? throw new ArgumentNullException(nameof(configuration));

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>();
    }

    private static void AddAuthentication(IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.ConfigureOptions<JwtOptionsSetup>();

        services.ConfigureOptions<JwtBearerOptionsSetup>();

        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<ISecureTokenService, SecureTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
    }
}
