using MassTransit;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Cart.Application.Carts.RemoveCart;

public sealed class OrderStatusChangedToPaidIntegrationEventHandler(
    ILogger<OrderStatusChangedToPaidIntegrationEventHandler> logger,
    ICommandHandler<RemoveCartCommand> handler)
    : IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderStatusChangedToPaidIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new RemoveCartCommand(context.Message.UserId);

        await handler.Handle(command, context.CancellationToken);
    }
}