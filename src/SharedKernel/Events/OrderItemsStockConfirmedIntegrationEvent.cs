namespace SharedKernel.Events;

public record OrderItemsStockConfirmedIntegrationEvent(long OrderId, List<PricedOrderItem> OrderItems) : IntegrationEvent;
