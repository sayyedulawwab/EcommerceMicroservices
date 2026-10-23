using Ordering.Domain.Orders;
using SharedKernel.Domain;

namespace Ordering.Domain.UnitTests.Orders;

public sealed class OrderConfirmStockTests
{
    [Fact]
    public void ConfirmStock_PlacedOrder_SetsNamesPricesAndStatus()
    {
        Order order = OrderTestData.PlacedOrder();

        Result result = order.ConfirmStock(OrderTestData.UsdPrices(), OrderTestData.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.StockConfirmed, order.Status);
        Assert.Equal(OrderTestData.UtcNow, order.UpdatedOnUtc);
        OrderItem keyboard = Assert.Single(order.OrderItems, item => item.ProductId == OrderTestData.KeyboardId);
        Assert.Equal(OrderTestData.KeyboardName, keyboard.ProductName);
        Assert.Equal(new Money(10m, Currency.Usd), keyboard.Price);
        OrderItem mouse = Assert.Single(order.OrderItems, item => item.ProductId == OrderTestData.MouseId);
        Assert.Equal(OrderTestData.MouseName, mouse.ProductName);
        Assert.Equal(new Money(5m, Currency.Usd), mouse.Price);
    }

    [Fact]
    public void ConfirmStock_SeveralItems_TotalIsSumOfPriceTimesQuantityForEveryItem()
    {
        // Regression: the first item's quantity used to be ignored (10 + 3 x 5 = 25 instead of 2 x 10 + 3 x 5 = 35).
        Order order = OrderTestData.PlacedOrder();

        Result result = order.ConfirmStock(OrderTestData.UsdPrices(), OrderTestData.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(new Money(35m, Currency.Usd), order.TotalPrice);
    }

    [Fact]
    public void ConfirmStock_SingleItemWithQuantityAboveOne_TotalIncludesQuantity()
    {
        List<OrderLine> lines = [new OrderLine(OrderTestData.KeyboardId, 4)];
        Order order = Order.Place(OrderTestData.UserId, lines, OrderTestData.CreatedOnUtc).Value;
        List<PricedOrderLine> prices = [new PricedOrderLine(OrderTestData.KeyboardId, OrderTestData.KeyboardName, new Money(12.5m, Currency.Bdt))];

        Result result = order.ConfirmStock(prices, OrderTestData.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(new Money(50m, Currency.Bdt), order.TotalPrice);
    }

    [Fact]
    public void ConfirmStock_PriceMissingForAnItem_ReturnsMissingPriceAndChangesNothing()
    {
        Order order = OrderTestData.PlacedOrder();
        List<PricedOrderLine> prices = [new PricedOrderLine(OrderTestData.KeyboardId, OrderTestData.KeyboardName, new Money(10m, Currency.Usd))];

        Result result = order.ConfirmStock(prices, OrderTestData.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.MissingPrice(OrderTestData.MouseId), result.Error);
        AssertUnconfirmed(order);
    }

    [Fact]
    public void ConfirmStock_MixedCurrencies_ReturnsMixedCurrenciesAndChangesNothing()
    {
        Order order = OrderTestData.PlacedOrder();
        List<PricedOrderLine> prices =
        [
            new PricedOrderLine(OrderTestData.KeyboardId, OrderTestData.KeyboardName, new Money(10m, Currency.Usd)),
            new PricedOrderLine(OrderTestData.MouseId, OrderTestData.MouseName, new Money(5m, Currency.Bdt)),
        ];

        Result result = order.ConfirmStock(prices, OrderTestData.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.MixedCurrencies, result.Error);
        AssertUnconfirmed(order);
    }

    [Fact]
    public void ConfirmStock_ExtraPricedProductNotInOrder_IsIgnored()
    {
        Order order = OrderTestData.PlacedOrder();
        List<PricedOrderLine> prices = OrderTestData.UsdPrices();
        prices.Add(new PricedOrderLine(Guid.CreateVersion7(), "Monitor", new Money(100m, Currency.Bdt)));

        Result result = order.ConfirmStock(prices, OrderTestData.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(new Money(35m, Currency.Usd), order.TotalPrice);
    }

    [Theory]
    [InlineData(OrderStatus.StockConfirmed)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void ConfirmStock_NotPlaced_ReturnsInvalidStatusTransitionAndChangesNothing(OrderStatus status)
    {
        Order order = OrderTestData.OrderIn(status);
        Money totalBefore = order.TotalPrice;
        DateTime? updatedBefore = order.UpdatedOnUtc;
        var pricesBefore = order.OrderItems.Select(item => item.Price).ToList();
        List<PricedOrderLine> newPrices =
        [
            new PricedOrderLine(OrderTestData.KeyboardId, "Other", new Money(1000m, Currency.Usd)),
            new PricedOrderLine(OrderTestData.MouseId, "Other", new Money(1000m, Currency.Usd)),
        ];

        Result result = order.ConfirmStock(newPrices, OrderTestData.UtcNow.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidStatusTransition(status, OrderStatus.StockConfirmed), result.Error);
        Assert.Equal(status, order.Status);
        Assert.Equal(totalBefore, order.TotalPrice);
        Assert.Equal(updatedBefore, order.UpdatedOnUtc);
        Assert.Equal(pricesBefore, order.OrderItems.Select(item => item.Price).ToList());
    }

    private static void AssertUnconfirmed(Order order)
    {
        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(Money.Zero(), order.TotalPrice);
        Assert.Null(order.UpdatedOnUtc);
        Assert.All(order.OrderItems, item =>
        {
            Assert.Equal(string.Empty, item.ProductName);
            Assert.Equal(Money.Zero(), item.Price);
        });
    }
}
