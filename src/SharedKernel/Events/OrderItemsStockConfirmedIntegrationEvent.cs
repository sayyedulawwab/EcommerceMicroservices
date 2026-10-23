namespace SharedKernel.Events;

public record OrderItemsStockConfirmedIntegrationEvent(Guid OrderId, List<PricedOrderItem> OrderItems) : IntegrationEvent;
