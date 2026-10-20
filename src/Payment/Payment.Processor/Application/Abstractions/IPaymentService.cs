namespace Payment.Processor.Application.Abstractions;

public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(long orderId, decimal amount, string currency, CancellationToken cancellationToken = default);
}
