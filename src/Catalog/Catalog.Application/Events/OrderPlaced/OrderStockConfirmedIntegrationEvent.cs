namespace Catalog.Application.Events.OrderPlaced;
internal record OrderStockConfirmedIntegrationEvent(long orderId) : IEvent;
