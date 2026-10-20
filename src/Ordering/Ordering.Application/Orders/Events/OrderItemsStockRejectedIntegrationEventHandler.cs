using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Application.Orders.CancelOrder;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.Events;

public sealed class OrderItemsStockRejectedIntegrationEventHandler(
    ILogger<OrderItemsStockRejectedIntegrationEventHandler> logger,
    ICommandHandler<CancelOrderCommand> handler)
    : IIntegrationEventHandler<OrderItemsStockRejectedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderItemsStockRejectedIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new CancelOrderCommand(context.Message.OrderId, $"Stock rejected: {context.Message.Reason}");

        Result result = await handler.Handle(command, context.CancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Cancelling after stock rejection for Order Id: {OrderId} failed: {@Error}", context.Message.OrderId, result.Error);
        }
    }
}
