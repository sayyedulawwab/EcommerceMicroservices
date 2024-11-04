namespace SharedLibrary.Events;

public record OrderPaymentSucceededIntegrationEvent(long orderId) : IEvent;
