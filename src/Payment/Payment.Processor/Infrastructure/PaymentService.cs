using Microsoft.Extensions.Options;
using Payment.Processor.Application.Abstractions;
using System.Security.Cryptography;

namespace Payment.Processor.Infrastructure;

/// <summary>
/// Simulated payment provider. <see cref="PaymentOptions"/> decide the outcome so both saga paths can be exercised.
/// </summary>
public class PaymentService(IOptionsMonitor<PaymentOptions> options, ILogger<PaymentService> logger) : IPaymentService
{
    private const int RandomScale = 10_000;

    public Task<PaymentResult> ProcessPaymentAsync(Guid orderId, decimal amount, string currency, CancellationToken cancellationToken = default)
    {
        PaymentOptions paymentOptions = options.CurrentValue;

        if (amount <= 0)
        {
            return Task.FromResult(PaymentResult.Failure("The amount to charge must be greater than zero"));
        }

        if (paymentOptions.FailAboveAmount is decimal limit && amount > limit)
        {
            return Task.FromResult(PaymentResult.Failure($"The amount {amount} {currency} exceeds the limit of {limit} {currency}"));
        }

        double roll = RandomNumberGenerator.GetInt32(RandomScale) / (double)RandomScale;

        if (roll >= paymentOptions.SuccessRate)
        {
            return Task.FromResult(PaymentResult.Failure("The payment was declined"));
        }

        logger.LogInformation("Charged {Amount} {Currency} for Order Id: {OrderId}", amount, currency, orderId);

        return Task.FromResult(PaymentResult.Success());
    }
}
