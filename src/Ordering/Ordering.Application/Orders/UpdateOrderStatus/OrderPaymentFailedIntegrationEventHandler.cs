using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
internal sealed class OrderPaymentFailedIntegrationEventHandler(
    ILogger<OrderPaymentFailedIntegrationEventHandler> logger,
    ISender sender)
    : IHandleMessages<OrderPaymentFailedIntegrationEvent>
{
    public async Task Handle(OrderPaymentFailedIntegrationEvent @event, IMessageHandlerContext context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", @event, @event.OrderId);

        var command = new UpdateOrderStatusToCanceledCommand(@event.OrderId);

        await sender.Send(command, default);
    }
}