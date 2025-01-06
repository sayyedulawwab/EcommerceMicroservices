using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
internal sealed class OrderItemsStockRejectedIntegrationEventHandler : IHandleMessages<OrderItemsStockRejectedIntegrationEvent>
{
    private readonly ILogger<OrderItemsStockRejectedIntegrationEventHandler> _logger;
    private readonly ISender _sender;

    public OrderItemsStockRejectedIntegrationEventHandler(ILogger<OrderItemsStockRejectedIntegrationEventHandler> logger, ISender sender)
    {
        _logger = logger;
        _sender = sender;
    }
    public async Task Handle(OrderItemsStockRejectedIntegrationEvent @event, IMessageHandlerContext context)
    {

        _logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", @event, @event.OrderId);

        var command = new UpdateOrderStatusToCanceledCommand(@event.OrderId);

        await _sender.Send(command, default);

    }
}