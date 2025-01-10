using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
public sealed class OrderPaymentFailedIntegrationEventHandler(
    ILogger<OrderPaymentFailedIntegrationEventHandler> logger,
    ISender sender)
    : IIntegrationEventHandler<OrderPaymentFailedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderPaymentFailedIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new UpdateOrderStatusToCanceledCommand(context.Message.OrderId);

        await sender.Send(command, default);
    }
}