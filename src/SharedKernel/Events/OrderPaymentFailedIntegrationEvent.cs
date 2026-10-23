namespace SharedKernel.Events;

public record OrderPaymentFailedIntegrationEvent(Guid OrderId, string Reason) : IntegrationEvent;
