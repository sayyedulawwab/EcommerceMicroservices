namespace SharedKernel.Events;

public record OrderItemsStockRejectedIntegrationEvent(Guid OrderId, string Reason, List<Guid> RejectedProductIds) : IntegrationEvent;
