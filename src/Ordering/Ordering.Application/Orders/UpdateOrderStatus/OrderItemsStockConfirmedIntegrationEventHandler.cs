using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
internal sealed class OrderItemsStockConfirmedIntegrationEventHandler : IHandleMessages<OrderItemsStockConfirmedIntegrationEvent>
{
    private readonly ILogger<OrderItemsStockConfirmedIntegrationEventHandler> _logger;
    private readonly ISender _sender;

    public OrderItemsStockConfirmedIntegrationEventHandler(ILogger<OrderItemsStockConfirmedIntegrationEventHandler> logger, ISender sender)
    {
        _logger = logger;
        _sender = sender;
    }
    public async Task Handle(OrderItemsStockConfirmedIntegrationEvent @event, IMessageHandlerContext context)
    {

        _logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", @event, @event.OrderId);

        var command = new UpdateOrderStatusToStockConfirmedCommand(@event.OrderId);

        await _sender.Send(command, default);

    }
}