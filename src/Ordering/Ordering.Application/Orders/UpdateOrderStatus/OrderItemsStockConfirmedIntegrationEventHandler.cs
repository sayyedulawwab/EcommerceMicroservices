using MassTransit;
using MassTransit.Middleware;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
public sealed class OrderItemsStockConfirmedIntegrationEventHandler(
    ILogger<OrderItemsStockConfirmedIntegrationEventHandler> logger,
    ISender sender)
    : IIntegrationEventHandler<OrderItemsStockConfirmedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderItemsStockConfirmedIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new UpdateOrderStatusToStockConfirmedCommand(context.Message.OrderId);

        await sender.Send(command, default);
    }
}