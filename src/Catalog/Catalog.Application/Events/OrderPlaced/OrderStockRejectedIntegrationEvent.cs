namespace Catalog.Application.Events.OrderPlaced;
internal record OrderStockRejectedIntegrationEvent(long orderId) : IEvent;
