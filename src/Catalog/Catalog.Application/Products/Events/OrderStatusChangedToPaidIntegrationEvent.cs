namespace Catalog.Application.Products.Events;
internal record OrderStatusChangedToPaidIntegrationEvent(long orderId, List<OrderStockItem> orderStockItems) : IEvent;
