using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
internal class OrderItemsStockConfirmedIntegrationEventHandler : IHandleMessages<OrderItemsStockConfirmedIntegrationEvent>
{
    private readonly ILogger<OrderItemsStockConfirmedIntegrationEvent> _logger;
    private readonly ISender _sender;

    public OrderItemsStockConfirmedIntegrationEventHandler(ILogger<OrderItemsStockConfirmedIntegrationEvent> logger, ISender sender)
    {
        _logger = logger;
        _sender = sender;
    }
    public async Task Handle(OrderItemsStockConfirmedIntegrationEvent @event, IMessageHandlerContext context)
    {

        _logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@orderId}", @event, @event.orderId);

        var command = new UpdateOrderStatusToStockConfirmedCommand(@event.orderId);

        await _sender.Send(command, default);

    }
}