namespace Catalog.Application.Products.Events;
internal record OrderItemsStockRejectedIntegrationEvent(long orderId) : IEvent;
