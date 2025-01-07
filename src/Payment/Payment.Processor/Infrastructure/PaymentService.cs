using Payment.Processor.Application.Abstractions;

namespace Payment.Processor.Infrastructure;

public class PaymentService : IPaymentService
{
    public Task<bool> ProcessPaymentAsync()
    {
        return Task.FromResult(true);
    }
}