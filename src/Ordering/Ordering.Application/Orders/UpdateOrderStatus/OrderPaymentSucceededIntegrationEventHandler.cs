using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
internal sealed class OrderPaymentSucceededIntegrationEventHandler(
    ILogger<OrderPaymentSucceededIntegrationEventHandler> logger,
    ISender sender)
    : IHandleMessages<OrderPaymentSucceededIntegrationEvent>
{
    public async Task Handle(OrderPaymentSucceededIntegrationEvent @event, IMessageHandlerContext context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", @event, @event.OrderId);

        var command = new UpdateOrderStatusToPaidCommand(@event.OrderId);

        await sender.Send(command, default);
    }
}