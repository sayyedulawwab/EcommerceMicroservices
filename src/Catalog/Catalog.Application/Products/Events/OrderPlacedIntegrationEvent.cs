namespace Catalog.Application.Products.Events;
internal record OrderPlacedIntegrationEvent(long orderId, List<OrderStockItem> orderStockItems) : IEvent;
