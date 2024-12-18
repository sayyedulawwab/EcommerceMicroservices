namespace SharedKernel.Events;
public record OrderStatusChangedToPaidIntegrationEvent(long userId, long orderId, List<OrderStockItem> orderStockItems) : IEvent;