using Payment.Processor.Middleware;

namespace Payment.Processor.Extensions;

public static class MiddlewareExtensions
{
    public static void UseCustomExceptionHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
    public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestContextLoggingMiddleware>();

        return app;
    }
}