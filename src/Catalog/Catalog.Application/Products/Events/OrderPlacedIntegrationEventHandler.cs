using Catalog.Domain.Products;
using MassTransit;
using MassTransit.Middleware;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;

namespace Catalog.Application.Products.Events;
public sealed class OrderPlacedIntegrationEventHandler(
    ILogger<OrderPlacedIntegrationEventHandler> logger,
    IProductRepository productRepository)
    : IIntegrationEventHandler<OrderPlacedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedIntegrationEvent> context)
    {
        logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", context.Message, context.Message.OrderId);

        // check stock
        var rejectedOrderStockItems = new List<OrderStockItem>();

        foreach (OrderStockItem orderStockItem in context.Message.OrderStockItems)
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

        if (rejectedOrderStockItems.Any())
        {
            var integrationEvent = new OrderItemsStockRejectedIntegrationEvent(context.Message.OrderId);

            // publish stock available event
            await context.Publish(integrationEvent);
        }
        else
        {
            var integrationEvent = new OrderItemsStockConfirmedIntegrationEvent(context.Message.OrderId);

            // publish stock available event
            await context.Publish(integrationEvent);
        }
    }
}