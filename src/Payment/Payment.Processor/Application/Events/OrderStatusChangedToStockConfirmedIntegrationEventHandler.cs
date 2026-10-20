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

        PaymentResult result = await paymentService.ProcessPaymentAsync(
            context.Message.OrderId,
            context.Message.TotalAmount,
            context.Message.Currency,
            context.CancellationToken);

        if (result.Succeeded)
        {
            await context.Publish(new OrderPaymentSucceededIntegrationEvent(context.Message.OrderId));
        }
        else
        {
            logger.LogWarning("Payment for Order Id: {OrderId} failed: {Reason}", context.Message.OrderId, result.FailureReason);

            await context.Publish(new OrderPaymentFailedIntegrationEvent(context.Message.OrderId, result.FailureReason ?? "Payment failed"));
        }
    }
}
