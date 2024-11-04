namespace SharedLibrary.Events;
public record OrderItemsStockConfirmedIntegrationEvent(long orderId) : IEvent;
