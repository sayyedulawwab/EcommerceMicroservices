using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
internal sealed class OrderPaymentSucceededIntegrationEventHandler : IHandleMessages<OrderPaymentSucceededIntegrationEvent>
{
    private readonly ILogger<OrderPaymentSucceededIntegrationEventHandler> _logger;
    private readonly ISender _sender;

    public OrderPaymentSucceededIntegrationEventHandler(ILogger<OrderPaymentSucceededIntegrationEventHandler> logger, ISender sender)
    {
        _logger = logger;
        _sender = sender;
    }
    public async Task Handle(OrderPaymentSucceededIntegrationEvent @event, IMessageHandlerContext context)
    {

        _logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", @event, @event.OrderId);


        var command = new UpdateOrderStatusToPaidCommand(@event.OrderId);

        await _sender.Send(command, default);

    }
}