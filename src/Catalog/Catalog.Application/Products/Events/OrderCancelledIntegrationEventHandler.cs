using Catalog.Application.Products.ReleaseStock;
using MassTransit;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.Events;

public sealed class OrderCancelledIntegrationEventHandler(
    ILogger<OrderCancelledIntegrationEventHandler> logger,
    ICommandHandler<ReleaseStockCommand> handler)
    : IIntegrationEventHandler<OrderCancelledIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelledIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        Result result = await handler.Handle(new ReleaseStockCommand(context.Message.OrderId), context.CancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Releasing stock for Order Id: {OrderId} failed: {@Error}", context.Message.OrderId, result.Error);
        }
    }
}
