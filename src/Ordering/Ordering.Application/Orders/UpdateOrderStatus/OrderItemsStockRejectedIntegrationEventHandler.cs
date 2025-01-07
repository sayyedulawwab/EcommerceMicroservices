using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
internal sealed class OrderItemsStockRejectedIntegrationEventHandler(
    ILogger<OrderItemsStockRejectedIntegrationEventHandler> logger,
    ISender sender)
    : IHandleMessages<OrderItemsStockRejectedIntegrationEvent>
{
    public async Task Handle(OrderItemsStockRejectedIntegrationEvent @event, IMessageHandlerContext context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", @event, @event.OrderId);

        var command = new UpdateOrderStatusToCanceledCommand(@event.OrderId);

        await sender.Send(command, default);
    }
}