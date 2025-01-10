using MassTransit;
using Ordering.Application.Abstractions.Clock;
using Ordering.Application.Abstractions.Messaging;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;

namespace Ordering.Application.Orders.PlaceOrder;
internal sealed class PlaceOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IPublishEndpoint publishEndpoint)
    : ICommandHandler<PlaceOrderCommand, long>
{
    public async Task<Result<long>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {

        var order = Order.Create(request.UserId, OrderStatus.Placed, dateTimeProvider.UtcNow);


        foreach (OrderItemCommand item in request.OrderItems)
        {
            order.AddOrderItem(order.Id,
                item.ProductId,
                item.ProductName,
                new Money(item.PriceAmount, Currency.Create(item.PriceCurrency)),
                item.Quantity,
                dateTimeProvider.UtcNow);
        }

        orderRepository.Add(order);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // publish event

        var orderStockItems = request.OrderItems.Select(orderItem => new OrderStockItem(
            orderItem.ProductId,
            orderItem.ProductName,
            orderItem.PriceAmount,
            orderItem.PriceCurrency,
            orderItem.Quantity)).ToList();


        var integrationEvent = new OrderPlacedIntegrationEvent(order.Id, orderStockItems);

        await publishEndpoint.Publish(integrationEvent, cancellationToken);

        return order.Id;
    }
}