namespace Cart.Application.Carts.Events;
internal record OrderStatusChangedToPaidIntegrationEvent(long orderId, List<OrderStockItem> orderStockItems) : IEvent;
