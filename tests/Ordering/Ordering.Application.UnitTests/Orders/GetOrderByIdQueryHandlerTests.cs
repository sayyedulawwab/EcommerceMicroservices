using Ordering.Application.Orders;
using Ordering.Application.Orders.GetOrderById;
using Ordering.Domain.Orders;
using SharedKernel.Domain;

namespace Ordering.Application.UnitTests.Orders;

public sealed class GetOrderByIdQueryHandlerTests : OrderHandlerTestBase
{
    [Fact]
    public async Task Handle_OwnerRequests_ReturnsOrder()
    {
        GivenOrder(TestOrders.In(OrderStatus.StockConfirmed));

        Result<OrderResponse> result = await CreateHandler().Handle(
            new GetOrderByIdQuery(TestOrders.OrderId, TestOrders.UserId, IsAdmin: false),
            CancellationToken);

        Assert.True(result.IsSuccess);
        OrderResponse response = result.Value;
        Assert.Equal(TestOrders.OrderId, response.Id);
        Assert.Equal(TestOrders.UserId, response.UserId);
        Assert.Equal(nameof(OrderStatus.StockConfirmed), response.Status);
        Assert.Equal(35m, response.TotalPriceAmount);
        Assert.Equal("USD", response.TotalPriceCurrency);
        Assert.Equal(2, response.OrderItems.Count);
        OrderItemResponse keyboard = Assert.Single(response.OrderItems, item => item.ProductId == TestOrders.KeyboardId);
        Assert.Equal("Keyboard", keyboard.ProductName);
        Assert.Equal(10m, keyboard.ProductPriceAmount);
        Assert.Equal(2, keyboard.Quantity);
    }

    [Fact]
    public async Task Handle_OtherNonAdminUser_ReturnsNotFound()
    {
        GivenOrder(TestOrders.Placed());

        Result<OrderResponse> result = await CreateHandler().Handle(
            new GetOrderByIdQuery(TestOrders.OrderId, TestOrders.OtherUserId, IsAdmin: false),
            CancellationToken);

        AssertNotFound(result);
    }

    [Fact]
    public async Task Handle_Admin_SeesAnyUsersOrder()
    {
        GivenOrder(TestOrders.Placed());

        Result<OrderResponse> result = await CreateHandler().Handle(
            new GetOrderByIdQuery(TestOrders.OrderId, TestOrders.OtherUserId, IsAdmin: true),
            CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(TestOrders.OrderId, result.Value.Id);
        Assert.Equal(TestOrders.UserId, result.Value.UserId);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ReturnsNotFoundEvenForAdmin()
    {
        GivenNoOrder(TestOrders.OrderId);

        Result<OrderResponse> result = await CreateHandler().Handle(
            new GetOrderByIdQuery(TestOrders.OrderId, TestOrders.UserId, IsAdmin: true),
            CancellationToken);

        AssertNotFound(result);
    }

    private GetOrderByIdQueryHandler CreateHandler()
    {
        return new GetOrderByIdQueryHandler(OrderRepository.Object);
    }
}
