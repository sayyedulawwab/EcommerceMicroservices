namespace SharedKernel.Events;

public record OrderStatusChangedToPaidIntegrationEvent(Guid UserId, Guid OrderId) : IntegrationEvent;
