using Payment.Processor.Application.Abstractions;
using SharedKernel.Events;

namespace Payment.Processor.Application.Events;
internal sealed class OrderStatusChangedToStockConfirmedIntegrationEventHandler(
    ILogger<OrderStatusChangedToStockConfirmedIntegrationEventHandler> logger,
    IPaymentService paymentService)
    : IIntegrationEventHandler<OrderStatusChangedToStockConfirmedIntegrationEvent>
{
    public async Task Handle(OrderStatusChangedToStockConfirmedIntegrationEvent @event, IMessageHandlerContext context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", @event, @event.OrderId);

        bool isPaymentSuccess = await paymentService.ProcessPaymentAsync();

        IEvent integrationEvent = isPaymentSuccess ? new OrderPaymentSucceededIntegrationEvent(@event.OrderId)
                                                : new OrderPaymentFailedIntegrationEvent(@event.OrderId);

        await context.Publish(integrationEvent);
    }
}