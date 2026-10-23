namespace SharedKernel.Events;

public record OrderStatusChangedToStockConfirmedIntegrationEvent(Guid OrderId, Guid UserId, decimal TotalAmount, string Currency) : IntegrationEvent;
