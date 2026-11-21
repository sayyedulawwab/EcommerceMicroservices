using SharedKernel.Domain;
using System.Net;

namespace SharedKernel.Exceptions;

/// <summary>
/// One error that groups several field errors, so a client gets all of them in one response.
/// </summary>
public sealed record ValidationError(IReadOnlyList<Error> Errors)
    : Error("Validation.General", "One or more validation errors occurred", HttpStatusCode.BadRequest)
{
    /// <summary>Collects the errors of the failed results, for example of several value objects created together.</summary>
    public static ValidationError Collect(params IEnumerable<Result> results) =>
        new([.. results.Where(result => result.IsFailure).Select(result => result.Error)]);
}
