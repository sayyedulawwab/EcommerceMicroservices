using MassTransit;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.UpdateOrderStatus;

public sealed class OrderPaymentSucceededIntegrationEventHandler(
    ILogger<OrderPaymentSucceededIntegrationEventHandler> logger,
    ICommandHandler<UpdateOrderStatusToPaidCommand, long> handler)
    : IIntegrationEventHandler<OrderPaymentSucceededIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderPaymentSucceededIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new UpdateOrderStatusToPaidCommand(context.Message.OrderId);

        await handler.Handle(command, default);
    }
}