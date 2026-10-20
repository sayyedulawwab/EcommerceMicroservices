using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Application.Orders.ConfirmStock;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.Events;

public sealed class OrderItemsStockConfirmedIntegrationEventHandler(
    ILogger<OrderItemsStockConfirmedIntegrationEventHandler> logger,
    ICommandHandler<ConfirmOrderStockCommand> handler)
    : IIntegrationEventHandler<OrderItemsStockConfirmedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderItemsStockConfirmedIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new ConfirmOrderStockCommand(context.Message.OrderId, context.Message.OrderItems);

        Result result = await handler.Handle(command, context.CancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Confirming stock for Order Id: {OrderId} failed: {@Error}", context.Message.OrderId, result.Error);
        }
    }
}
