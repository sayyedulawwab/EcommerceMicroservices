namespace SharedLibrary.Events;
public record OrderItemsStockRejectedIntegrationEvent(long orderId) : IEvent;
