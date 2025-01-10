using MassTransit;
using Payment.Processor.Application.Abstractions;
using SharedKernel.Events;

namespace Payment.Processor.Application.Events;
public sealed class OrderStatusChangedToStockConfirmedIntegrationEventHandler(
    ILogger<OrderStatusChangedToStockConfirmedIntegrationEventHandler> logger,
    IPaymentService paymentService)
    : IIntegrationEventHandler<OrderStatusChangedToStockConfirmedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderStatusChangedToStockConfirmedIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        bool isPaymentSuccess = await paymentService.ProcessPaymentAsync();

        if (isPaymentSuccess)
        {
            var integrationEvent = new OrderPaymentSucceededIntegrationEvent(context.Message.OrderId);

            await context.Publish(integrationEvent);
        }
        else
        {
            var integrationEvent = new OrderPaymentFailedIntegrationEvent(context.Message.OrderId);

            await context.Publish(integrationEvent);
        }
    }
}