using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Cart.Application.Carts.RemoveCart;
internal class OrderStatusChangedToPaidIntegrationEventHandler : IHandleMessages<OrderStatusChangedToPaidIntegrationEvent>
{
    private readonly ILogger<OrderStatusChangedToPaidIntegrationEvent> _logger;
    private readonly ISender _sender;

    public OrderStatusChangedToPaidIntegrationEventHandler(ILogger<OrderStatusChangedToPaidIntegrationEvent> logger, ISender sender)
    {
        _logger = logger;
        _sender = sender;
    }
    public async Task Handle(OrderStatusChangedToPaidIntegrationEvent @event, IMessageHandlerContext context)
    {

        _logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@orderId}", @event, @event.orderId);

        var command = new RemoveCartCommand(@event.userId);

        await _sender.Send(command);
    }
}
