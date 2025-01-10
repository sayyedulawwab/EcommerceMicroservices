namespace SharedKernel.Events;
public record OrderStatusChangedToPaidIntegrationEvent(long UserId, long OrderId, List<OrderStockItem> OrderStockItems) : IntegrationEvent;