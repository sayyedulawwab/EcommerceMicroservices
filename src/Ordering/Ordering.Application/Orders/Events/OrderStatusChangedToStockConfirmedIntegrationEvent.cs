namespace Ordering.Application.Orders.Events;
internal record OrderStatusChangedToStockConfirmedIntegrationEvent(long orderId) : IEvent;
