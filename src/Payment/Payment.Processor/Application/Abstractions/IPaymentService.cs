namespace Payment.Processor.Application.Abstractions;

public interface IPaymentService
{
    Task<bool> ProcessPaymentAsync();
}