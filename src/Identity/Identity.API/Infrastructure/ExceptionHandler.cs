using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Exceptions;

namespace Identity.API.Infrastructure;

/// <summary>
/// Turns an exception that escaped a controller into a problem-details response.
/// Expected failures are returned as <c>Result</c> errors and never get here.
/// </summary>
internal sealed class ExceptionHandler(
    ILogger<ExceptionHandler> logger,
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        if (exception is ConcurrencyException)
        {
            logger.LogWarning(exception, "Concurrency conflict");

            // Someone else saved the same row first; the client can reload and retry.
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Type = "ConcurrencyConflict",
                Title = "Conflict",
                Detail = "The resource was changed by another request. Reload it and try again."
            };
        }
        else
        {
            logger.LogError(exception, "Unhandled exception");

            // The exception message can contain SQL or table names, so the client only gets a fixed text.
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Type = "ServerError",
                Title = "Server error",
                Detail = "An unexpected error has occurred"
            };
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
