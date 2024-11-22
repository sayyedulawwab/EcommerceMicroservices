namespace SharedKernel.Events;
public record OrderStatusChangedToPaidIntegrationEvent(long orderId, List<OrderStockItem> orderStockItems) : IEvent;