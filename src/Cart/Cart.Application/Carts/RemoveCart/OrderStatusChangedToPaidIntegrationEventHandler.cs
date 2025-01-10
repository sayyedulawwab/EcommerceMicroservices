using MassTransit;
using MassTransit.Middleware;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Cart.Application.Carts.RemoveCart;
public sealed class OrderStatusChangedToPaidIntegrationEventHandler(
    ILogger<OrderStatusChangedToPaidIntegrationEventHandler> logger,
    ISender sender)
    : IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderStatusChangedToPaidIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new RemoveCartCommand(context.Message.UserId);

        await sender.Send(command, context.CancellationToken);
    }
}