namespace SharedKernel.Events;
public record OrderPlacedIntegrationEvent(long OrderId, List<OrderStockItem> OrderStockItems) : IntegrationEvent;