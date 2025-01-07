using Catalog.Domain.Products;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Catalog.Application.Products.Events;
internal sealed class OrderPlacedIntegrationEventHandler(
    ILogger<OrderPlacedIntegrationEventHandler> logger,
    IProductRepository productRepository)
    : IHandleMessages<OrderPlacedIntegrationEvent>
{
    public async Task Handle(OrderPlacedIntegrationEvent @event, IMessageHandlerContext context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", @event, @event.OrderId);

        // check stock
        var rejectedOrderStockItems = new List<OrderStockItem>();

        foreach (OrderStockItem orderStockItem in @event.OrderStockItems)
        {
            Product? product = await productRepository.GetByIdAsync(orderStockItem.ProductId, context.CancellationToken);

            if (product is null)
            {
                logger.LogWarning("Product with Id: {ProductId} not found", orderStockItem.ProductId);
                rejectedOrderStockItems.Add(orderStockItem);
                continue;
            }

            bool hasStock = product.Quantity >= orderStockItem.Quantity;

            if (!hasStock)
            {
                rejectedOrderStockItems.Add(orderStockItem);
            }
        }

        IEvent integrationEvent = rejectedOrderStockItems.Any() ? new OrderItemsStockRejectedIntegrationEvent(@event.OrderId)
                                                                : new OrderItemsStockConfirmedIntegrationEvent(@event.OrderId);

        // publish stock available event
        await context.Publish(integrationEvent);
    }
}