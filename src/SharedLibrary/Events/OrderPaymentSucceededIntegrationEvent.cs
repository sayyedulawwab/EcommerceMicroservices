namespace SharedLibrary.Events;

public record OrderPaymentFailedIntegrationEvent(long orderId) : IEvent;
