using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
internal sealed class OrderItemsStockConfirmedIntegrationEventHandler(
    ILogger<OrderItemsStockConfirmedIntegrationEventHandler> logger,
    ISender sender)
    : IIntegrationEventHandler<OrderItemsStockConfirmedIntegrationEvent>
{
    public async Task Handle(OrderItemsStockConfirmedIntegrationEvent @event, IMessageHandlerContext context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", @event, @event.OrderId);

        var command = new UpdateOrderStatusToStockConfirmedCommand(@event.OrderId);

        await sender.Send(command, default);
    }
}