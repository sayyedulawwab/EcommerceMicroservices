using MassTransit;
using MassTransit.Middleware;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
public sealed class OrderItemsStockRejectedIntegrationEventHandler(
    ILogger<OrderItemsStockRejectedIntegrationEventHandler> logger,
    ISender sender)
    : IIntegrationEventHandler<OrderItemsStockRejectedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderItemsStockRejectedIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new UpdateOrderStatusToCanceledCommand(context.Message.OrderId);

        await sender.Send(command, default);
    }
}