namespace Ordering.Application.Orders.Events;
internal record OrderStatusChangedToPaidIntegrationEvent(long orderId, List<OrderStockItem> orderStockItems) : IEvent;
