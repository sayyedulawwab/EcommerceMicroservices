using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Cart.Application.Carts.RemoveCart;
internal sealed class OrderStatusChangedToPaidIntegrationEventHandler(
    ILogger<OrderStatusChangedToPaidIntegrationEventHandler> logger,
    ISender sender)
    : IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
{
    public async Task Handle(OrderStatusChangedToPaidIntegrationEvent @event, IMessageHandlerContext context)
    {

        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", @event, @event.OrderId);

        var command = new RemoveCartCommand(@event.UserId);

        await sender.Send(command, context.CancellationToken);
    }
}