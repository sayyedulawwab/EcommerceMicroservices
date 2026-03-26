using MassTransit;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.UpdateOrderStatus;

public sealed class OrderItemsStockRejectedIntegrationEventHandler(
    ILogger<OrderItemsStockRejectedIntegrationEventHandler> logger,
    ICommandHandler<UpdateOrderStatusToCanceledCommand, long> handler)
    : IIntegrationEventHandler<OrderItemsStockRejectedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderItemsStockRejectedIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new UpdateOrderStatusToCanceledCommand(context.Message.OrderId);

        await handler.Handle(command, default);
    }
}