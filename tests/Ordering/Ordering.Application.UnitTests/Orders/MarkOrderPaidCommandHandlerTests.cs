using Moq;
using Ordering.Application.Orders.MarkOrderPaid;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;

namespace Ordering.Application.UnitTests.Orders;

public sealed class MarkOrderPaidCommandHandlerTests : OrderHandlerTestBase
{
    [Fact]
    public async Task Handle_StockConfirmedOrder_MarksPaidAndPublishesEvent()
    {
        Order order = TestOrders.In(OrderStatus.StockConfirmed);
        GivenOrder(order);

        Result result = await CreateHandler().Handle(new MarkOrderPaidCommand(TestOrders.OrderId), CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(TestOrders.UtcNow, order.UpdatedOnUtc);
        OrderStatusChangedToPaidIntegrationEvent paid = Assert.IsType<OrderStatusChangedToPaidIntegrationEvent>(Assert.Single(PublishedMessages));
        Assert.Equal(TestOrders.UserId, paid.UserId);
        Assert.Equal(TestOrders.OrderId, paid.OrderId);
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task Handle_AlreadyPaid_SucceedsWithoutPublishing()
    {
        Order order = TestOrders.In(OrderStatus.Paid);
        GivenOrder(order);

        Result result = await CreateHandler().Handle(new MarkOrderPaidCommand(TestOrders.OrderId), CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Empty(PublishedMessages);
        VerifySaved(Times.Never());
    }

    [Theory]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.Shipped)]
    public async Task Handle_NotStockConfirmed_ReturnsInvalidStatusTransition(OrderStatus status)
    {
        Order order = TestOrders.In(status);
        GivenOrder(order);

        Result result = await CreateHandler().Handle(new MarkOrderPaidCommand(TestOrders.OrderId), CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidStatusTransition(status, OrderStatus.Paid), result.Error);
        Assert.Equal(status, order.Status);
        Assert.Empty(PublishedMessages);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task Handle_UnknownOrder_ReturnsNotFound()
    {
        GivenNoOrder(TestOrders.OrderId);

        Result result = await CreateHandler().Handle(new MarkOrderPaidCommand(TestOrders.OrderId), CancellationToken);

        AssertNotFound(result);
        Assert.Empty(PublishedMessages);
    }

    private MarkOrderPaidCommandHandler CreateHandler()
    {
        return new MarkOrderPaidCommandHandler(OrderRepository.Object, UnitOfWork.Object, Clock.Object, PublishEndpoint.Object);
    }
}
