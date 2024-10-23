namespace Ordering.Application.Orders.Events;
internal record OrderItemsStockConfirmedIntegrationEvent(long orderId) : IEvent;
