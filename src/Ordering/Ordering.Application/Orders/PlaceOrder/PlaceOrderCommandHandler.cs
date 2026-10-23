using MassTransit;
using Ordering.Application.Abstractions.Clock;
using Ordering.Application.Abstractions.Data;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.PlaceOrder;

internal sealed class PlaceOrderCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IPublishEndpoint publishEndpoint)
    : ICommandHandler<PlaceOrderCommand, long>
{
    public async Task<Result<long>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var lines = request.OrderItems
            .Select(item => new OrderLine(item.ProductId, item.Quantity))
            .ToList();

        Result<Order> orderResult = Order.Place(request.UserId, lines, dateTimeProvider.UtcNow);

        if (orderResult.IsFailure)
        {
            return Result.Failure<long>(orderResult.Error);
        }

        Order order = orderResult.Value;

        // The event needs the database-generated order id, so the order is saved first. The second save stores the
        // event in the outbox; the transaction makes the order and its event commit together.
        await dbContext.ExecuteInTransactionAsync(async ct =>
        {
            dbContext.Orders.Add(order);

            await dbContext.SaveChangesAsync(ct);

            var orderStockItems = order.OrderItems
                .Select(item => new OrderStockItem(item.ProductId, item.Quantity))
                .ToList();

            await publishEndpoint.Publish(new OrderPlacedIntegrationEvent(order.Id, order.UserId, orderStockItems), ct);

            await dbContext.SaveChangesAsync(ct);
        }, cancellationToken);

        return order.Id;
    }
}
