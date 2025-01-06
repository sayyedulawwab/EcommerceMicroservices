using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Cart.Application.Carts.RemoveCart;
internal sealed class OrderStatusChangedToPaidIntegrationEventHandler : IHandleMessages<OrderStatusChangedToPaidIntegrationEvent>
{
    private readonly ILogger<OrderStatusChangedToPaidIntegrationEventHandler> _logger;
    private readonly ISender _sender;

    public OrderStatusChangedToPaidIntegrationEventHandler(ILogger<OrderStatusChangedToPaidIntegrationEventHandler> logger, ISender sender)
    {
        _logger = logger;
        _sender = sender;
    }
    public async Task Handle(OrderStatusChangedToPaidIntegrationEvent @event, IMessageHandlerContext context)
    {

        _logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", @event, @event.OrderId);

        var command = new RemoveCartCommand(@event.UserId);

        await _sender.Send(command, context.CancellationToken);
    }
}
