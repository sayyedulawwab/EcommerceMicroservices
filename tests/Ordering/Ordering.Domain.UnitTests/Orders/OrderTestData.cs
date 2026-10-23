using Ordering.Domain.Orders;
using SharedKernel.Domain;

namespace Ordering.Domain.UnitTests.Orders;

internal static class OrderTestData
{
    public static readonly Guid UserId = Guid.CreateVersion7();
    public static readonly Guid KeyboardId = Guid.CreateVersion7();
    public static readonly Guid MouseId = Guid.CreateVersion7();
    public const string KeyboardName = "Keyboard";
    public const string MouseName = "Mouse";

    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime UtcNow = new(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);

    // 2 keyboards at 10 USD and 3 mice at 5 USD: total 35 USD.
    public static Order PlacedOrder()
    {
        List<OrderLine> lines = [new OrderLine(KeyboardId, 2), new OrderLine(MouseId, 3)];

        return Order.Place(UserId, lines, CreatedOnUtc).Value;
    }

    public static List<PricedOrderLine> UsdPrices()
    {
        return
        [
            new PricedOrderLine(KeyboardId, KeyboardName, new Money(10m, Currency.Usd)),
            new PricedOrderLine(MouseId, MouseName, new Money(5m, Currency.Usd)),
        ];
    }

    public static Order OrderIn(OrderStatus status)
    {
        Order order = PlacedOrder();

        if (status == OrderStatus.Placed)
        {
            return order;
        }

        if (status == OrderStatus.Cancelled)
        {
            AssertSuccess(order.Cancel("Changed my mind", UtcNow));
            return order;
        }

        AssertSuccess(order.ConfirmStock(UsdPrices(), UtcNow));

        if (status == OrderStatus.StockConfirmed)
        {
            return order;
        }

        AssertSuccess(order.MarkPaid(UtcNow));

        if (status == OrderStatus.Paid)
        {
            return order;
        }

        AssertSuccess(order.Ship(UtcNow));

        if (status == OrderStatus.Shipped)
        {
            return order;
        }

        AssertSuccess(order.Deliver(UtcNow));

        return order;
    }

    private static void AssertSuccess(Result result)
    {
        Assert.True(result.IsSuccess, $"Arranging the order failed: {result.Error.Name}");
    }
}
