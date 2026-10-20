namespace Payment.Processor.Application.Abstractions;

public sealed record PaymentResult(bool Succeeded, string? FailureReason)
{
    public static PaymentResult Success() => new(true, null);

    public static PaymentResult Failure(string reason) => new(false, reason);
}
