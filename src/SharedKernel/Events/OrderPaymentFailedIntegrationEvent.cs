namespace SharedKernel.Events;

public record OrderPaymentFailedIntegrationEvent(long OrderId, string Reason) : IntegrationEvent;
