namespace Payment.Processor.Application.Events;

public record OrderPaymentSucceededIntegrationEvent(long orderId) : IEvent;
