namespace SharedKernel.Events;
public record OrderStatusChangedToStockConfirmedIntegrationEvent(long orderId) : IEvent;