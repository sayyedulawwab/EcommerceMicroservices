
using Catalog.Domain.Products;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Events.OrderPlaced;
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
        _logger.LogInformation("Placed Order: {orderId}", @event.orderId);

        // check stock

        var hasStock = true;

        foreach (var orderStockItem in @event.orderStockItems)
        {
            var product = await _productRepository.GetByIdAsync(orderStockItem.productId);

            if (product is null)
            {
                hasStock = false;
                break;
            }
            else if (product.Quantity < orderStockItem.quantity) 
            {
                hasStock = false;
                break;
            }

        }

  

        // publish stock available event
    }
}
