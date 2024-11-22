using Catalog.Domain.Products;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Catalog.Application.Products.Events;
internal class OrderPlacedIntegrationEventHandler : IHandleMessages<OrderPlacedIntegrationEvent>
{
    private readonly ILogger<OrderPlacedIntegrationEvent> _logger;
    private readonly IProductRepository _productRepository;

    public OrderPlacedIntegrationEventHandler(ILogger<OrderPlacedIntegrationEvent> logger, IProductRepository productRepository)
    {
        _logger = logger;
        _productRepository = productRepository;
    }
    public async Task Handle(OrderPlacedIntegrationEvent @event, IMessageHandlerContext context)
    {

        _logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@orderId}", @event, @event.orderId);

        // check stock

        var rejectedOrderStockItems = new List<OrderStockItem>();

        foreach (var orderStockItem in @event.orderStockItems)
        {
            var product = await _productRepository.GetByIdAsync(orderStockItem.productId);

            var hasStock = product.Quantity >= orderStockItem.quantity;

            if (!hasStock)
            {
                rejectedOrderStockItems.Add(orderStockItem);
            }

        }

        var integrationEvent = rejectedOrderStockItems.Any() ? (IEvent)new OrderItemsStockRejectedIntegrationEvent(@event.orderId)
                                                              : new OrderItemsStockConfirmedIntegrationEvent(@event.orderId);


        // publish stock available event

        await context.Publish(integrationEvent);
    }
}