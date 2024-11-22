namespace SharedKernel.Events;
public record OrderItemsStockConfirmedIntegrationEvent(long orderId) : IEvent;