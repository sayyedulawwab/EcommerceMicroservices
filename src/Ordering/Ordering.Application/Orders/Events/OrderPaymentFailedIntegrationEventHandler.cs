using MediatR;
using Microsoft.Extensions.Logging;
using Ordering.Application.Orders.Commands.UpdateOrderStatus;

namespace Ordering.Application.Orders.Events;
internal class OrderPaymentFailedIntegrationEventHandler : IHandleMessages<OrderPaymentFailedIntegrationEvent>
{
    private readonly ILogger<OrderPaymentFailedIntegrationEvent> _logger;
    private readonly ISender _sender;

    public OrderPaymentFailedIntegrationEventHandler(ILogger<OrderPaymentFailedIntegrationEvent> logger, ISender sender)
    {
        _logger = logger;
        _sender = sender;
    }
    public async Task Handle(OrderPaymentFailedIntegrationEvent @event, IMessageHandlerContext context)
    {

        _logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@orderId}", @event, @event.orderId);

        var command = new UpdateOrderStatusToCanceledCommand(@event.orderId);

        await _sender.Send(command);
    }
}
