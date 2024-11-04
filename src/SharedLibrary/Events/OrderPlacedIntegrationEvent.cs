namespace SharedLibrary.Events;
public record OrderPlacedIntegrationEvent(long orderId, List<OrderStockItem> orderStockItems) : IEvent;
