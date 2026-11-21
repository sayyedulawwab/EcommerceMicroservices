namespace SharedKernel.Domain;

/// <summary>
/// The outcome of an operation that can be rejected by a business rule.
/// A result is a success exactly when its error is <see cref="Error.None"/>.
/// </summary>
public class Result
{
    private protected Result(Error error)
    {
        Error = error;
    }

    public Error Error { get; }

    public bool IsSuccess => Error == Error.None;

    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(Error.None);

    public static Result Failure(Error error) => new(RequireFailure(error));

    public static Result<TValue> Success<TValue>(TValue value) => new(value);

    public static Result<TValue> Failure<TValue>(Error error) => new(RequireFailure(error));

    private protected static Error RequireFailure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return error == Error.None
            ? throw new ArgumentException("A failed result needs an error other than Error.None.", nameof(error))
            : error;
    }
}

/// <summary>
/// A result that carries a value when it succeeds.
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue value)
        : base(Error.None)
    {
        _value = value;
    }

    internal Result(Error error)
        : base(error)
    {
    }

    // Reading the value of a failure is a bug in the caller, not a business outcome.
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"A failed result has no value. Error: {Error.Name}.");

    // A handler can return its response directly; a null response becomes Error.NullValue.
    public static implicit operator Result<TValue>(TValue? value) =>
        value is null ? new Result<TValue>(Error.NullValue) : new Result<TValue>(value);
}
