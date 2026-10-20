namespace SharedKernel.Events;

public record OrderPlacedIntegrationEvent(long OrderId, long UserId, List<OrderStockItem> OrderStockItems) : IntegrationEvent;
