using SharedKernel.Domain;

namespace SharedKernel.Exceptions;

public sealed record ValidationError : Error
{
    public ValidationError(Error[] errors)
        : base(
            "Validation.General",
            "One or more validation errors occurred",
            HttpResponseStatusCodes.BadRequest)
    {
        Errors = errors;
    }

    public Error[] Errors { get; }

    public static ValidationError FromResults(IEnumerable<Result> results) =>
        new(results.Where(r => r.IsFailure).Select(r => r.Error).ToArray());
}