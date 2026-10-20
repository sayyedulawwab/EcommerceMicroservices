using Catalog.API.Middleware;

namespace Catalog.API.Extensions;

public static class MiddlewareExtensions
{
    public static void UseCustomExceptionHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
    public static IApplicationBuilder UseCsrfProtection(this IApplicationBuilder app)
    {
        app.UseMiddleware<CsrfProtectionMiddleware>();

        return app;
    }

    public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestContextLoggingMiddleware>();

        return app;
    }
}