namespace SharedKernel.Events;

public record OrderPaymentSucceededIntegrationEvent(long orderId) : IEvent;