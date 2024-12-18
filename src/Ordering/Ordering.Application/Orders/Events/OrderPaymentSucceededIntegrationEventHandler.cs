using MediatR;
using Microsoft.Extensions.Logging;
using Ordering.Application.Orders.Commands.UpdateOrderStatus;
using SharedKernel.Events;

namespace Ordering.Application.Orders.Events;
internal class OrderPaymentSucceededIntegrationEventHandler : IHandleMessages<OrderPaymentSucceededIntegrationEvent>
{
    private readonly ILogger<OrderPaymentSucceededIntegrationEvent> _logger;
    private readonly ISender _sender;

    public OrderPaymentSucceededIntegrationEventHandler(ILogger<OrderPaymentSucceededIntegrationEvent> logger, ISender sender)
    {
        _logger = logger;
        _sender = sender;
    }
    public async Task Handle(OrderPaymentSucceededIntegrationEvent @event, IMessageHandlerContext context)
    {

        _logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@orderId}", @event, @event.orderId);


        var command = new UpdateOrderStatusToPaidCommand(@event.orderId);

        await _sender.Send(command);

    }
}