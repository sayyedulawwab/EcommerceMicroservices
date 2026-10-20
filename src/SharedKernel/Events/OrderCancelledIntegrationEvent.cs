namespace SharedKernel.Events;

/// <summary>
/// Published whenever an order ends up cancelled. Catalog releases any stock it reserved for the order;
/// the release is idempotent, so this may be published more than once for the same order.
/// </summary>
public record OrderCancelledIntegrationEvent(long OrderId, string Reason) : IntegrationEvent;
