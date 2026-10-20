using Moq;
using Ordering.Application.Orders.CancelOrder;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;

namespace Ordering.Application.UnitTests.Orders;

public sealed class CancelOrderCommandHandlerTests : OrderHandlerTestBase
{
    private const string Reason = "No longer needed";

    [Theory]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.StockConfirmed)]
    public async Task Handle_CustomerCancelsOwnOrder_CancelsAndPublishesOrderCancelled(OrderStatus status)
    {
        Order order = TestOrders.In(status);
        GivenOrder(order);

        Result result = await CreateHandler().Handle(new CancelOrderCommand(TestOrders.OrderId, Reason, TestOrders.UserId), CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(Reason, order.CancellationReason);
        Assert.Equal(TestOrders.UtcNow, order.UpdatedOnUtc);
        OrderCancelledIntegrationEvent cancelled = Assert.IsType<OrderCancelledIntegrationEvent>(Assert.Single(PublishedMessages));
        Assert.Equal(new OrderCancelledIntegrationEvent(TestOrders.OrderId, Reason), cancelled);
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task Handle_SystemCancellation_CancelsRegardlessOfOwner()
    {
        Order order = TestOrders.Placed();
        GivenOrder(order);

        Result result = await CreateHandler().Handle(new CancelOrderCommand(TestOrders.OrderId, Reason), CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.IsType<OrderCancelledIntegrationEvent>(Assert.Single(PublishedMessages));
    }

    [Fact]
    public async Task Handle_AnotherUsersOrder_ReturnsNotFoundAndLeavesOrderAlone()
    {
        Order order = TestOrders.Placed();
        GivenOrder(order);

        Result result = await CreateHandler().Handle(new CancelOrderCommand(TestOrders.OrderId, Reason, TestOrders.OtherUserId), CancellationToken);

        AssertNotFound(result);
        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Empty(PublishedMessages);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task Handle_UnknownOrder_ReturnsNotFound()
    {
        GivenNoOrder(TestOrders.OrderId);

        Result result = await CreateHandler().Handle(new CancelOrderCommand(TestOrders.OrderId, Reason), CancellationToken);

        AssertNotFound(result);
        Assert.Empty(PublishedMessages);
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_SucceedsWithoutPublishing()
    {
        Order order = TestOrders.In(OrderStatus.Cancelled);
        GivenOrder(order);

        Result result = await CreateHandler().Handle(new CancelOrderCommand(TestOrders.OrderId, Reason, TestOrders.UserId), CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(TestOrders.CustomerCancelReason, order.CancellationReason);
        Assert.Empty(PublishedMessages);
        VerifySaved(Times.Never());
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task Handle_OrderPastCancellation_ReturnsCannotCancelAndPublishesNothing(OrderStatus status)
    {
        Order order = TestOrders.In(status);
        GivenOrder(order);

        Result result = await CreateHandler().Handle(new CancelOrderCommand(TestOrders.OrderId, Reason, TestOrders.UserId), CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.CannotCancel(status), result.Error);
        Assert.Equal(status, order.Status);
        Assert.Empty(PublishedMessages);
        VerifySaved(Times.Never());
    }

    private CancelOrderCommandHandler CreateHandler()
    {
        return new CancelOrderCommandHandler(OrderRepository.Object, UnitOfWork.Object, Clock.Object, PublishEndpoint.Object);
    }
}
