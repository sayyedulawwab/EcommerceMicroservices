using Catalog.Application.Products.UpdateProductStock;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Catalog.Application.Products.Events;
internal sealed class OrderStatusChangedToPaidIntegrationEventHandler(
    ILogger<OrderStatusChangedToPaidIntegrationEventHandler> logger,
    ISender sender)
    : IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
{
    public async Task Handle(OrderStatusChangedToPaidIntegrationEvent @event, IMessageHandlerContext context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", @event, @event.OrderId);

        foreach (OrderStockItem orderStockItem in @event.OrderStockItems)
        {

            var command = new UpdateProductStockCommand(orderStockItem.ProductId, orderStockItem.Quantity);

            await sender.Send(command, context.CancellationToken);
        }
    }
}