using MediatR;
using Microsoft.Extensions.Logging;
using Ordering.Application.Orders.Commands.UpdateOrderStatus;
using SharedKernel.Events;

namespace Ordering.Application.Orders.Events;
internal class OrderItemsStockRejectedIntegrationEventHandler : IHandleMessages<OrderItemsStockRejectedIntegrationEvent>
{
    private readonly ILogger<OrderItemsStockRejectedIntegrationEvent> _logger;
    private readonly ISender _sender;

    public OrderItemsStockRejectedIntegrationEventHandler(ILogger<OrderItemsStockRejectedIntegrationEvent> logger, ISender sender)
    {
        _logger = logger;
        _sender = sender;
    }
    public async Task Handle(OrderItemsStockRejectedIntegrationEvent @event, IMessageHandlerContext context)
    {

        _logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@orderId}", @event, @event.orderId);

        var command = new UpdateOrderStatusToCanceledCommand(@event.orderId);

        await _sender.Send(command);

    }
}