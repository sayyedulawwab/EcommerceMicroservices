namespace Payment.Processor.Application.Events;
internal record OrderStatusChangedToStockConfirmedIntegrationEvent(long orderId) : IEvent;
