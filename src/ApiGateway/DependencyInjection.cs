namespace ApiGateway;

public static class DependencyInjection
{
    public const string FrontendCorsPolicy = "frontend";

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        string[] allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        // The frontend is on another site and authenticates with cookies, so only listed origins may send
        // credentialed requests. Services reject cookie-authenticated writes that skip the preflight (see CsrfProtectionMiddleware).
        services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowCredentials()
            .AllowAnyMethod()
            .WithHeaders("Content-Type", "X-Requested-With", "X-Api-Version", "X-Correlation-Id", "Authorization")));

        services.AddHealthChecks();

        return services;
    }
}
