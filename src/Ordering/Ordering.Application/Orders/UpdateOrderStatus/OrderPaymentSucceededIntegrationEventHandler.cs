using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
public sealed class OrderPaymentSucceededIntegrationEventHandler(
    ILogger<OrderPaymentSucceededIntegrationEventHandler> logger,
    ISender sender)
    : IIntegrationEventHandler<OrderPaymentSucceededIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderPaymentSucceededIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new UpdateOrderStatusToPaidCommand(context.Message.OrderId);

        await sender.Send(command, default);
    }
}