using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Application.Orders.MarkOrderPaid;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.Events;

public sealed class OrderPaymentSucceededIntegrationEventHandler(
    ILogger<OrderPaymentSucceededIntegrationEventHandler> logger,
    ICommandHandler<MarkOrderPaidCommand> handler)
    : IIntegrationEventHandler<OrderPaymentSucceededIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderPaymentSucceededIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new MarkOrderPaidCommand(context.Message.OrderId);

        Result result = await handler.Handle(command, context.CancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Marking the order paid (a refund may be needed if it was cancelled meanwhile) for Order Id: {OrderId} failed: {@Error}", context.Message.OrderId, result.Error);
        }
    }
}
