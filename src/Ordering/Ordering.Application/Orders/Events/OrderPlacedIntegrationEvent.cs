namespace Ordering.Application.Orders.Events;
internal record OrderPlacedIntegrationEvent(long orderId, List<OrderStockItem> orderStockItems) : IEvent;
