using EcommerceMicroservices.IntegrationTests.Infrastructure;
using Ordering.Domain.Orders;
using SharedKernel.Events;
using System.Net;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests.Ordering;

/// <summary>A customer cancelling an order over HTTP.</summary>
[Collection(SystemCollection.Name)]
public sealed class CancelOrderTests(SystemFixture system)
{
    private const string CancelledByCustomer = "Cancelled by customer";

    [Theory]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.StockConfirmed)]
    public async Task CancelOrder_CustomerCancelsOwnOrder_CancelsItAndPublishesOrderCancelled(OrderStatus status)
    {
        Guid userId = TestJwt.NewUserId();
        Guid orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, userId, status);
        using HttpClient customer = OrderingApi.CustomerClient(system, userId);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderCancelledIntegrationEvent> cancelled = await BusProbe<OrderCancelledIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        HttpResponseMessage response = await OrderingApi.PostAsync(customer, $"/api/v1/orders/{orderId}/cancel");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        OrderCancelledIntegrationEvent @event = await cancelled.WaitForAsync(e => e.OrderId == orderId, "OrderCancelled to be published");
        Assert.Equal(CancelledByCustomer, @event.Reason);
        JsonElement order = await OrderingApi.GetOrderAsync(admin, orderId);
        Assert.Equal("Cancelled", order.GetProperty("status").GetString());
        Assert.Equal(CancelledByCustomer, order.GetProperty("cancellationReason").GetString());
        Assert.NotEqual(JsonValueKind.Null, order.GetProperty("updatedOnUtc").ValueKind);
    }

    [Fact]
    public async Task CancelOrder_AnotherUsersOrder_GetsNotFound_AndLeavesTheOrderAlone()
    {
        Guid orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), OrderStatus.Placed);
        using HttpClient stranger = OrderingApi.CustomerClient(system, TestJwt.NewUserId());
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderCancelledIntegrationEvent> cancelled = await BusProbe<OrderCancelledIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        HttpResponseMessage response = await OrderingApi.PostAsync(stranger, $"/api/v1/orders/{orderId}/cancel");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await cancelled.ExpectNoneAsync(e => e.OrderId == orderId));
        Assert.Equal("Placed", await OrderingApi.GetStatusAsync(admin, orderId));
    }

    [Fact]
    public async Task CancelOrder_UnknownOrder_GetsNotFound()
    {
        using HttpClient customer = OrderingApi.CustomerClient(system, TestJwt.NewUserId());

        HttpResponseMessage response = await OrderingApi.PostAsync(customer, $"/api/v1/orders/{Guid.CreateVersion7()}/cancel");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_AlreadyCancelled_Succeeds_WithoutPublishingAgain()
    {
        Guid userId = TestJwt.NewUserId();
        Guid orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, userId, OrderStatus.Cancelled);
        using HttpClient customer = OrderingApi.CustomerClient(system, userId);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderCancelledIntegrationEvent> cancelled = await BusProbe<OrderCancelledIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        HttpResponseMessage response = await OrderingApi.PostAsync(customer, $"/api/v1/orders/{orderId}/cancel");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await cancelled.ExpectNoneAsync(e => e.OrderId == orderId));
        JsonElement order = await OrderingApi.GetOrderAsync(admin, orderId);
        Assert.Equal(OrderingTestData.SeededCancellationReason, order.GetProperty("cancellationReason").GetString());
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task CancelOrder_OrderPastCancellation_IsAConflict_AndPublishesNothing(OrderStatus status)
    {
        Guid userId = TestJwt.NewUserId();
        Guid orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, userId, status);
        using HttpClient customer = OrderingApi.CustomerClient(system, userId);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderCancelledIntegrationEvent> cancelled = await BusProbe<OrderCancelledIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        HttpResponseMessage response = await OrderingApi.PostAsync(customer, $"/api/v1/orders/{orderId}/cancel");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty(await cancelled.ExpectNoneAsync(e => e.OrderId == orderId));
        Assert.Equal(status.ToString(), await OrderingApi.GetStatusAsync(admin, orderId));
    }
}
