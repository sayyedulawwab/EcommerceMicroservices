using MassTransit;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.UpdateOrderStatus;

public sealed class OrderPaymentFailedIntegrationEventHandler(
    ILogger<OrderPaymentFailedIntegrationEventHandler> logger,
    ICommandHandler<UpdateOrderStatusToCanceledCommand, long> handler)
    : IIntegrationEventHandler<OrderPaymentFailedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderPaymentFailedIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        var command = new UpdateOrderStatusToCanceledCommand(context.Message.OrderId);

        await handler.Handle(command, default);
    }
}