using Catalog.Application.Products.ReserveStock;
using MassTransit;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.Events;

public sealed class OrderPlacedIntegrationEventHandler(
    ILogger<OrderPlacedIntegrationEventHandler> logger,
    ICommandHandler<ReserveStockCommand, ReserveStockResult> handler)
    : IIntegrationEventHandler<OrderPlacedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new ReserveStockCommand(context.Message.OrderId, context.Message.OrderStockItems);

        Result<ReserveStockResult> result = await handler.Handle(command, context.CancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Stock reservation for Order Id: {OrderId} failed: {@Error}", context.Message.OrderId, result.Error);

            await context.Publish(new OrderItemsStockRejectedIntegrationEvent(context.Message.OrderId, result.Error.Description, []));

            return;
        }

        if (result.Value.IsReserved)
        {
            await context.Publish(new OrderItemsStockConfirmedIntegrationEvent(context.Message.OrderId, [.. result.Value.Items]));
        }
        else
        {
            await context.Publish(new OrderItemsStockRejectedIntegrationEvent(
                context.Message.OrderId,
                result.Value.RejectionReason ?? "Stock rejected",
                [.. result.Value.RejectedProductIds]));
        }
    }
}
