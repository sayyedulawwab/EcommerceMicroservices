namespace SharedLibrary.Events;
public record OrderStatusChangedToStockConfirmedIntegrationEvent(long orderId) : IEvent;
