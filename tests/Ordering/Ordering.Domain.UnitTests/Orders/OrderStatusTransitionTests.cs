using Ordering.Domain.Orders;
using SharedKernel.Domain;

namespace Ordering.Domain.UnitTests.Orders;

public sealed class OrderStatusTransitionTests
{
    private static readonly DateTime Later = OrderTestData.UtcNow.AddDays(1);

    [Fact]
    public void MarkPaid_StockConfirmed_MovesToPaid()
    {
        Order order = OrderTestData.OrderIn(OrderStatus.StockConfirmed);

        Result result = order.MarkPaid(Later);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(Later, order.UpdatedOnUtc);
    }

    [Theory]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void MarkPaid_NotStockConfirmed_ReturnsInvalidStatusTransition(OrderStatus status)
    {
        Order order = OrderTestData.OrderIn(status);
        DateTime? updatedBefore = order.UpdatedOnUtc;

        Result result = order.MarkPaid(Later);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidStatusTransition(status, OrderStatus.Paid), result.Error);
        Assert.Equal(status, order.Status);
        Assert.Equal(updatedBefore, order.UpdatedOnUtc);
    }

    [Fact]
    public void Ship_Paid_MovesToShipped()
    {
        Order order = OrderTestData.OrderIn(OrderStatus.Paid);

        Result result = order.Ship(Later);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal(Later, order.UpdatedOnUtc);
    }

    [Theory]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.StockConfirmed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void Ship_NotPaid_ReturnsInvalidStatusTransition(OrderStatus status)
    {
        Order order = OrderTestData.OrderIn(status);

        Result result = order.Ship(Later);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidStatusTransition(status, OrderStatus.Shipped), result.Error);
        Assert.Equal(status, order.Status);
    }

    [Fact]
    public void Deliver_Shipped_MovesToDelivered()
    {
        Order order = OrderTestData.OrderIn(OrderStatus.Shipped);

        Result result = order.Deliver(Later);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(Later, order.UpdatedOnUtc);
    }

    [Theory]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.StockConfirmed)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void Deliver_NotShipped_ReturnsInvalidStatusTransition(OrderStatus status)
    {
        Order order = OrderTestData.OrderIn(status);

        Result result = order.Deliver(Later);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidStatusTransition(status, OrderStatus.Delivered), result.Error);
        Assert.Equal(status, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.StockConfirmed)]
    public void Cancel_CancellableStatus_MovesToCancelledAndStoresReason(OrderStatus status)
    {
        Order order = OrderTestData.OrderIn(status);

        Result result = order.Cancel("Found it cheaper", Later);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Found it cheaper", order.CancellationReason);
        Assert.Equal(Later, order.UpdatedOnUtc);
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public void Cancel_NonCancellableStatus_ReturnsCannotCancel(OrderStatus status)
    {
        Order order = OrderTestData.OrderIn(status);
        DateTime? updatedBefore = order.UpdatedOnUtc;

        Result result = order.Cancel("Too late", Later);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.CannotCancel(status), result.Error);
        Assert.Equal(status, order.Status);
        Assert.Null(order.CancellationReason);
        Assert.Equal(updatedBefore, order.UpdatedOnUtc);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ReturnsCannotCancel()
    {
        Order order = OrderTestData.OrderIn(OrderStatus.Cancelled);

        Result result = order.Cancel("Again", Later);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.CannotCancel(OrderStatus.Cancelled), result.Error);
        Assert.Equal("Changed my mind", order.CancellationReason);
    }

    [Fact]
    public void Cancel_ReasonLongerThanMax_TruncatesReason()
    {
        Order order = OrderTestData.PlacedOrder();
        string reason = new('x', Order.MaxCancellationReasonLength + 25);

        Result result = order.Cancel(reason, Later);

        Assert.True(result.IsSuccess);
        Assert.Equal(new string('x', Order.MaxCancellationReasonLength), order.CancellationReason);
    }

    [Fact]
    public void Cancel_ReasonExactlyMaxLength_KeepsWholeReason()
    {
        Order order = OrderTestData.PlacedOrder();
        string reason = new('y', Order.MaxCancellationReasonLength);

        Result result = order.Cancel(reason, Later);

        Assert.True(result.IsSuccess);
        Assert.Equal(reason, order.CancellationReason);
    }

    [Theory]
    [InlineData(OrderStatus.Placed, true)]
    [InlineData(OrderStatus.StockConfirmed, true)]
    [InlineData(OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Cancelled, false)]
    public void CanBeCancelled_ReflectsStatus(OrderStatus status, bool expected)
    {
        Order order = OrderTestData.OrderIn(status);

        Assert.Equal(expected, order.CanBeCancelled);
    }

    [Fact]
    public void FullLifecycle_PlacedToDelivered_Succeeds()
    {
        Order order = OrderTestData.PlacedOrder();

        Assert.True(order.ConfirmStock(OrderTestData.UsdPrices(), Later).IsSuccess);
        Assert.True(order.MarkPaid(Later).IsSuccess);
        Assert.True(order.Ship(Later).IsSuccess);
        Assert.True(order.Deliver(Later).IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }
}
