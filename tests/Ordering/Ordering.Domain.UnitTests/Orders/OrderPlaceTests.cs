using Ordering.Domain.Orders;
using SharedKernel.Domain;

namespace Ordering.Domain.UnitTests.Orders;

public sealed class OrderPlaceTests
{
    [Fact]
    public void Place_NoLines_ReturnsEmptyError()
    {
        Result<Order> result = Order.Place(OrderTestData.UserId, [], OrderTestData.CreatedOnUtc);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.Empty, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Place_NonPositiveQuantity_ReturnsInvalidQuantityError(int quantity)
    {
        List<OrderLine> lines = [new OrderLine(OrderTestData.KeyboardId, 1), new OrderLine(OrderTestData.MouseId, quantity)];

        Result<Order> result = Order.Place(OrderTestData.UserId, lines, OrderTestData.CreatedOnUtc);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidQuantity(OrderTestData.MouseId), result.Error);
    }

    [Fact]
    public void Place_ValidLines_CreatesPlacedOrderWithZeroTotal()
    {
        List<OrderLine> lines = [new OrderLine(OrderTestData.KeyboardId, 2), new OrderLine(OrderTestData.MouseId, 3)];

        Result<Order> result = Order.Place(OrderTestData.UserId, lines, OrderTestData.CreatedOnUtc);

        Assert.True(result.IsSuccess);
        Order order = result.Value;
        Assert.Equal(OrderTestData.UserId, order.UserId);
        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(Money.Zero(), order.TotalPrice);
        Assert.Equal(OrderTestData.CreatedOnUtc, order.CreatedOnUtc);
        Assert.Null(order.UpdatedOnUtc);
        Assert.Null(order.CancellationReason);
        Assert.Equal(2, order.OrderItems.Count);
        Assert.Contains(order.OrderItems, item => item.ProductId == OrderTestData.KeyboardId && item.Quantity == 2);
        Assert.Contains(order.OrderItems, item => item.ProductId == OrderTestData.MouseId && item.Quantity == 3);
    }

    [Fact]
    public void Place_ValidLines_ItemsHaveNoNameOrPriceYet()
    {
        Order order = OrderTestData.PlacedOrder();

        Assert.All(order.OrderItems, item =>
        {
            Assert.Equal(string.Empty, item.ProductName);
            Assert.Equal(Money.Zero(), item.Price);
            Assert.Equal(OrderTestData.CreatedOnUtc, item.CreatedOnUtc);
        });
    }

    [Fact]
    public void Place_DuplicateProductLines_MergesIntoOneItemWithSummedQuantity()
    {
        List<OrderLine> lines =
        [
            new OrderLine(OrderTestData.KeyboardId, 2),
            new OrderLine(OrderTestData.MouseId, 1),
            new OrderLine(OrderTestData.KeyboardId, 3),
        ];

        Result<Order> result = Order.Place(OrderTestData.UserId, lines, OrderTestData.CreatedOnUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.OrderItems.Count);
        OrderItem keyboard = Assert.Single(result.Value.OrderItems, item => item.ProductId == OrderTestData.KeyboardId);
        Assert.Equal(5, keyboard.Quantity);
        OrderItem mouse = Assert.Single(result.Value.OrderItems, item => item.ProductId == OrderTestData.MouseId);
        Assert.Equal(1, mouse.Quantity);
    }
}
