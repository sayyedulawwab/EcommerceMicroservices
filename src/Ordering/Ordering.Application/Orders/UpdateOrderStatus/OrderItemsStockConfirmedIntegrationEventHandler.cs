using MassTransit;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.UpdateOrderStatus;

public sealed class OrderItemsStockConfirmedIntegrationEventHandler(
    ILogger<OrderItemsStockConfirmedIntegrationEventHandler> logger,
    ICommandHandler<UpdateOrderStatusToStockConfirmedCommand, long> handler)
    : IIntegrationEventHandler<OrderItemsStockConfirmedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderItemsStockConfirmedIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new UpdateOrderStatusToStockConfirmedCommand(context.Message.OrderId);

        await handler.Handle(command, default);
    }
}