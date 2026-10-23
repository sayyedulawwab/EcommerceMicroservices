using Ordering.Domain.Orders;
using SharedKernel.Domain;

namespace Ordering.Domain.UnitTests.Orders;

public sealed class OrderItemTests
{
    [Fact]
    public void Create_SetsProductQuantityAndZeroPrice()
    {
        var item = OrderItem.Create(OrderId.New(), OrderTestData.KeyboardId, 3, OrderTestData.CreatedOnUtc);

        Assert.NotEqual(Guid.Empty, item.Id.Value);
        Assert.Equal(OrderTestData.KeyboardId, item.ProductId);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(string.Empty, item.ProductName);
        Assert.Equal(Money.Zero(), item.Price);
        Assert.Equal(OrderTestData.CreatedOnUtc, item.CreatedOnUtc);
    }

    [Fact]
    public void LineTotal_BeforePricing_IsZero()
    {
        var item = OrderItem.Create(OrderId.New(), OrderTestData.KeyboardId, 3, OrderTestData.CreatedOnUtc);

        Assert.Equal(Money.Zero(), item.LineTotal);
    }

    [Fact]
    public void LineTotal_AfterPricing_IsPriceTimesQuantity()
    {
        Order order = OrderTestData.OrderIn(OrderStatus.StockConfirmed);

        OrderItem keyboard = Assert.Single(order.OrderItems, item => item.ProductId == OrderTestData.KeyboardId);
        OrderItem mouse = Assert.Single(order.OrderItems, item => item.ProductId == OrderTestData.MouseId);
        Assert.Equal(new Money(20m, Currency.Usd), keyboard.LineTotal);
        Assert.Equal(new Money(15m, Currency.Usd), mouse.LineTotal);
    }
}
