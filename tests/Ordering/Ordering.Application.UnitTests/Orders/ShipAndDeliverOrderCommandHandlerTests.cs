using Moq;
using Ordering.Application.Orders.DeliverOrder;
using Ordering.Application.Orders.ShipOrder;
using Ordering.Domain.Orders;
using SharedKernel.Domain;

namespace Ordering.Application.UnitTests.Orders;

public sealed class ShipAndDeliverOrderCommandHandlerTests : OrderHandlerTestBase
{
    [Fact]
    public async Task Ship_PaidOrder_MovesToShippedAndSaves()
    {
        Order order = TestOrders.In(OrderStatus.Paid);
        GivenOrder(order);

        Result result = await CreateShipHandler().Handle(new ShipOrderCommand(TestOrders.OrderId), CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal(TestOrders.UtcNow, order.UpdatedOnUtc);
        VerifySaved(Times.Once());
    }

    [Theory]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.StockConfirmed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task Ship_NotPaid_ReturnsInvalidStatusTransitionWithoutSaving(OrderStatus status)
    {
        Order order = TestOrders.In(status);
        GivenOrder(order);

        Result result = await CreateShipHandler().Handle(new ShipOrderCommand(TestOrders.OrderId), CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidStatusTransition(status, OrderStatus.Shipped), result.Error);
        Assert.Equal(status, order.Status);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task Ship_UnknownOrder_ReturnsNotFound()
    {
        GivenNoOrder(TestOrders.OrderId);

        Result result = await CreateShipHandler().Handle(new ShipOrderCommand(TestOrders.OrderId), CancellationToken);

        AssertNotFound(result);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task Deliver_ShippedOrder_MovesToDeliveredAndSaves()
    {
        Order order = TestOrders.In(OrderStatus.Shipped);
        GivenOrder(order);

        Result result = await CreateDeliverHandler().Handle(new DeliverOrderCommand(TestOrders.OrderId), CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(TestOrders.UtcNow, order.UpdatedOnUtc);
        VerifySaved(Times.Once());
    }

    [Theory]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task Deliver_NotShipped_ReturnsInvalidStatusTransitionWithoutSaving(OrderStatus status)
    {
        Order order = TestOrders.In(status);
        GivenOrder(order);

        Result result = await CreateDeliverHandler().Handle(new DeliverOrderCommand(TestOrders.OrderId), CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidStatusTransition(status, OrderStatus.Delivered), result.Error);
        Assert.Equal(status, order.Status);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task Deliver_UnknownOrder_ReturnsNotFound()
    {
        GivenNoOrder(TestOrders.OrderId);

        Result result = await CreateDeliverHandler().Handle(new DeliverOrderCommand(TestOrders.OrderId), CancellationToken);

        AssertNotFound(result);
        VerifySaved(Times.Never());
    }

    private ShipOrderCommandHandler CreateShipHandler()
    {
        return new ShipOrderCommandHandler(OrderRepository.Object, UnitOfWork.Object, Clock.Object);
    }

    private DeliverOrderCommandHandler CreateDeliverHandler()
    {
        return new DeliverOrderCommandHandler(OrderRepository.Object, UnitOfWork.Object, Clock.Object);
    }
}
