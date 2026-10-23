using MassTransit;
using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions.Clock;
using Ordering.Application.Abstractions.Data;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.ConfirmStock;

internal sealed class ConfirmOrderStockCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IPublishEndpoint publishEndpoint)
    : ICommandHandler<ConfirmOrderStockCommand>
{
    public async Task<Result> Handle(ConfirmOrderStockCommand request, CancellationToken cancellationToken)
    {
        Order? order = await dbContext.Orders
            .Include(order => order.OrderItems)
            .FirstOrDefaultAsync(order => order.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure(OrderErrors.NotFound());
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            // The customer cancelled while Catalog was reserving: ask Catalog to give the stock back.
            await publishEndpoint.Publish(
                new OrderCancelledIntegrationEvent(order.Id, order.CancellationReason ?? "Order was cancelled"),
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        if (order.Status != OrderStatus.Placed)
        {
            // Duplicate delivery; the stock was already confirmed.
            return Result.Success();
        }

        Result confirmResult = ConfirmStock(order, request.OrderItems);

        if (confirmResult.IsFailure)
        {
            // Bad data from Catalog would otherwise leave the order Placed with its stock reserved forever.
            order.Cancel($"Stock confirmation was invalid: {confirmResult.Error.Description}", dateTimeProvider.UtcNow);

            await publishEndpoint.Publish(
                new OrderCancelledIntegrationEvent(order.Id, order.CancellationReason ?? confirmResult.Error.Description),
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            return confirmResult;
        }

        await publishEndpoint.Publish(
            new OrderStatusChangedToStockConfirmedIntegrationEvent(
                order.Id,
                order.UserId,
                order.TotalPrice.Amount,
                order.TotalPrice.Currency.Code),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private Result ConfirmStock(Order order, List<PricedOrderItem> orderItems)
    {
        PricedOrderItem? unsupported = orderItems.Find(item => !Currency.IsSupported(item.PriceCurrency));

        if (unsupported is not null)
        {
            return Result.Failure(OrderErrors.UnsupportedCurrency(unsupported.PriceCurrency));
        }

        var pricedLines = orderItems
            .Select(item => new PricedOrderLine(
                item.ProductId,
                item.ProductName,
                new Money(item.PriceAmount, Currency.FromCode(item.PriceCurrency))))
            .ToList();

        return order.ConfirmStock(pricedLines, dateTimeProvider.UtcNow);
    }
}
