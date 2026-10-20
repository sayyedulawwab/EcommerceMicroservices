using Catalog.Application.Products.CommitStock;
using MassTransit;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.Events;

public sealed class OrderStatusChangedToPaidIntegrationEventHandler(
    ILogger<OrderStatusChangedToPaidIntegrationEventHandler> logger,
    ICommandHandler<CommitStockCommand> handler)
    : IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderStatusChangedToPaidIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        Result result = await handler.Handle(new CommitStockCommand(context.Message.OrderId), context.CancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Committing stock for Order Id: {OrderId} failed: {@Error}", context.Message.OrderId, result.Error);
        }
    }
}
