using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
internal sealed class OrderPaymentFailedIntegrationEventHandler : IHandleMessages<OrderPaymentFailedIntegrationEvent>
{
    private readonly ILogger<OrderPaymentFailedIntegrationEventHandler> _logger;
    private readonly ISender _sender;

    public OrderPaymentFailedIntegrationEventHandler(ILogger<OrderPaymentFailedIntegrationEventHandler> logger, ISender sender)
    {
        _logger = logger;
        _sender = sender;
    }
    public async Task Handle(OrderPaymentFailedIntegrationEvent @event, IMessageHandlerContext context)
    {

        _logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", @event, @event.OrderId);

        var command = new UpdateOrderStatusToCanceledCommand(@event.OrderId);

        await _sender.Send(command, default);
    }
}