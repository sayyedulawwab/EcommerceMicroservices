namespace SharedKernel.Events;
public record OrderItemsStockRejectedIntegrationEvent(long orderId) : IEvent;