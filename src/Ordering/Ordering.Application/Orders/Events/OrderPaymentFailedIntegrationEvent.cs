namespace Ordering.Application.Orders.Events;

public record OrderPaymentFailedIntegrationEvent(long orderId) : IEvent;
