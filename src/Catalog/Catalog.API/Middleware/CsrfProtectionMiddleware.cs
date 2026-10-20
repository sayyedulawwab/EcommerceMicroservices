using Microsoft.AspNetCore.Mvc;
using SharedKernel.Auth;

namespace Catalog.API.Middleware;

/// <summary>
/// Auth cookies are SameSite=None (the frontend is cross-site), so the browser attaches them to forged requests too.
/// A cookie-authenticated request that changes state must therefore carry something a plain HTML form or a "simple"
/// cross-site fetch can't send: a JSON content type or an X-Requested-With header. Both force a CORS preflight,
/// which the gateway only allows for configured origins. Bearer-token clients carry no cookies and are not affected.
/// </summary>
public class CsrfProtectionMiddleware(RequestDelegate next)
{
    private const string RequestedWithHeaderName = "X-Requested-With";

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsStateChanging(context.Request) && HasAuthCookie(context.Request) && !ForcesPreflight(context.Request))
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Type = "CsrfCheckFailed",
                Title = "Forbidden",
                Detail = $"Cookie-authenticated requests that change state must send 'Content-Type: application/json' or an '{RequestedWithHeaderName}' header."
            };

            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);

            return;
        }

        await next(context);
    }

    private static bool IsStateChanging(HttpRequest request) =>
        !HttpMethods.IsGet(request.Method) &&
        !HttpMethods.IsHead(request.Method) &&
        !HttpMethods.IsOptions(request.Method) &&
        !HttpMethods.IsTrace(request.Method);

    private static bool HasAuthCookie(HttpRequest request) =>
        request.Cookies.ContainsKey(AuthCookies.AccessToken) ||
        request.Cookies.ContainsKey(AuthCookies.RefreshToken);

    private static bool ForcesPreflight(HttpRequest request) =>
        request.Headers.ContainsKey(RequestedWithHeaderName) || request.HasJsonContentType();
}
