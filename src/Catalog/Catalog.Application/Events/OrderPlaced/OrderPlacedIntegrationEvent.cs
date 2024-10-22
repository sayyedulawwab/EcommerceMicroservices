namespace Catalog.Application.Events.OrderPlaced;
internal record OrderPlacedIntegrationEvent(long orderId, List<OrderStockItem> orderStockItems) : IEvent;
