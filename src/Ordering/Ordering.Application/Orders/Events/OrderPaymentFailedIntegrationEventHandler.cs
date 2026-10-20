using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Application.Orders.CancelOrder;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.Events;

public sealed class OrderPaymentFailedIntegrationEventHandler(
    ILogger<OrderPaymentFailedIntegrationEventHandler> logger,
    ICommandHandler<CancelOrderCommand> handler)
    : IIntegrationEventHandler<OrderPaymentFailedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderPaymentFailedIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new CancelOrderCommand(context.Message.OrderId, $"Payment failed: {context.Message.Reason}");

        Result result = await handler.Handle(command, context.CancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Cancelling after failed payment for Order Id: {OrderId} failed: {@Error}", context.Message.OrderId, result.Error);
        }
    }
}
