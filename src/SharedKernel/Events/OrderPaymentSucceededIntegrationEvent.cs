namespace SharedKernel.Events;

public record OrderPaymentFailedIntegrationEvent(long orderId) : IEvent;