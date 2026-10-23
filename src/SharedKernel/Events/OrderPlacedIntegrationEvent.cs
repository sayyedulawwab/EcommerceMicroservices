namespace SharedKernel.Events;

public record OrderPlacedIntegrationEvent(Guid OrderId, Guid UserId, List<OrderStockItem> OrderStockItems) : IntegrationEvent;
