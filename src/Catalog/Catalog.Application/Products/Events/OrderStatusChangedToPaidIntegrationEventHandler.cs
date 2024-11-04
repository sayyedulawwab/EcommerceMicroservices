using Catalog.Application.Products.Commands.EditProduct;
using Catalog.Domain.Products;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Events;

namespace Catalog.Application.Products.Events;
internal class OrderStatusChangedToPaidIntegrationEventHandler : IHandleMessages<OrderStatusChangedToPaidIntegrationEvent>
{
    private readonly ILogger<OrderStatusChangedToPaidIntegrationEvent> _logger;
    private readonly ISender _sender;

    public OrderStatusChangedToPaidIntegrationEventHandler(ILogger<OrderStatusChangedToPaidIntegrationEvent> logger, ISender sender)
    {
        _logger = logger;
        _sender = sender;
    }
    public async Task Handle(OrderStatusChangedToPaidIntegrationEvent @event, IMessageHandlerContext context)
    {

        _logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@orderId}", @event, @event.orderId);

        foreach (var orderStockItem in @event.orderStockItems)
        {

            var command = new UpdateProductStockCommand(orderStockItem.productId, orderStockItem.quantity);

            await _sender.Send(command);
        }

    }
}
