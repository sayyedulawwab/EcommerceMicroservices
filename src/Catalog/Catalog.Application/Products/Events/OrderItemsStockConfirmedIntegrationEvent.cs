namespace Catalog.Application.Products.Events;
internal record OrderItemsStockConfirmedIntegrationEvent(long orderId) : IEvent;
