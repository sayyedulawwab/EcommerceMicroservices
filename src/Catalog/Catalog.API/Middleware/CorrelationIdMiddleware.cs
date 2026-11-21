using Serilog.Context;

namespace Catalog.API.Middleware;

/// <summary>
/// Gives every request a correlation ID, puts it on every Serilog event written while the request runs,
/// and returns it in the response so a client can quote it when reporting a problem.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        // Reuse the caller's ID when it sends one, so a request can be followed across services.
        string correlationId = context.Request.Headers[HeaderName] is [{ Length: > 0 } fromCaller]
            ? fromCaller
            : context.TraceIdentifier;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // The property must stay pushed until the rest of the pipeline has finished, hence the await inside.
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
