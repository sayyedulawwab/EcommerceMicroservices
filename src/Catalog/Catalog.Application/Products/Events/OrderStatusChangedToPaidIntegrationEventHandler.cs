using Catalog.Application.Products.UpdateProductStock;
using MassTransit;
using MassTransit.Middleware;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Catalog.Application.Products.Events;
public sealed class OrderStatusChangedToPaidIntegrationEventHandler(
    ILogger<OrderStatusChangedToPaidIntegrationEventHandler> logger,
    ISender sender)
    : IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderStatusChangedToPaidIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        foreach (OrderStockItem orderStockItem in context.Message.OrderStockItems)
        {
            var command = new UpdateProductStockCommand(orderStockItem.ProductId, orderStockItem.Quantity);

            await sender.Send(command, context.CancellationToken);
        }
    }
}