namespace SharedKernel.Events;

public record OrderItemsStockRejectedIntegrationEvent(long OrderId, string Reason, List<long> RejectedProductIds) : IntegrationEvent;
