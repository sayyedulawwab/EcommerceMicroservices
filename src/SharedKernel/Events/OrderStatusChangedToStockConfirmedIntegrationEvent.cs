namespace SharedKernel.Events;

public record OrderStatusChangedToStockConfirmedIntegrationEvent(long OrderId, long UserId, decimal TotalAmount, string Currency) : IntegrationEvent;
