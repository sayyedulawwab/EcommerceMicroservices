namespace Payment.Processor.Application.Events;

public record OrderPaymentFailedIntegrationEvent(long orderId) : IEvent;
