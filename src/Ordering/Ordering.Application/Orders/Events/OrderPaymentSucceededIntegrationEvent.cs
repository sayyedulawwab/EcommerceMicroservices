namespace Ordering.Application.Orders.Events;

public record OrderPaymentSucceededIntegrationEvent(long orderId) : IEvent;
