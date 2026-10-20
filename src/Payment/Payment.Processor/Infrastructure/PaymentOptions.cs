namespace Payment.Processor.Infrastructure;

public sealed class PaymentOptions
{
    public const string SectionName = "Payment";

    /// <summary>Share of payments that succeed, from 0 (all fail) to 1 (all succeed).</summary>
    public double SuccessRate { get; set; } = 1;

    /// <summary>Payments above this amount always fail. Null disables the limit.</summary>
    public decimal? FailAboveAmount { get; set; }
}
