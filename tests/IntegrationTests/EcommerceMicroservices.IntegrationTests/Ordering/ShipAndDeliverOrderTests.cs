using EcommerceMicroservices.IntegrationTests.Infrastructure;
using Ordering.Domain.Orders;
using System.Net;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests.Ordering;

/// <summary>An admin shipping and delivering orders over HTTP.</summary>
[Collection(SystemCollection.Name)]
public sealed class ShipAndDeliverOrderTests(SystemFixture system)
{
    [Fact]
    public async Task ShipOrder_PaidOrder_MovesToShipped()
    {
        long orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), OrderStatus.Paid);
        using HttpClient admin = OrderingApi.AdminClient(system);

        HttpResponseMessage response = await OrderingApi.PostAsync(admin, $"/api/v1/orders/{orderId}/ship");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        JsonElement order = await OrderingApi.GetOrderAsync(admin, orderId);
        Assert.Equal("Shipped", order.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, order.GetProperty("updatedOnUtc").ValueKind);
    }

    [Theory]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.StockConfirmed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task ShipOrder_NotPaid_IsAConflict_AndLeavesTheOrderAlone(OrderStatus status)
    {
        long orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), status);
        using HttpClient admin = OrderingApi.AdminClient(system);

        HttpResponseMessage response = await OrderingApi.PostAsync(admin, $"/api/v1/orders/{orderId}/ship");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(status.ToString(), await OrderingApi.GetStatusAsync(admin, orderId));
    }

    [Fact]
    public async Task ShipOrder_UnknownOrder_GetsNotFound()
    {
        using HttpClient admin = OrderingApi.AdminClient(system);

        HttpResponseMessage response = await OrderingApi.PostAsync(admin, $"/api/v1/orders/{long.MaxValue}/ship");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeliverOrder_ShippedOrder_MovesToDelivered()
    {
        long orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), OrderStatus.Shipped);
        using HttpClient admin = OrderingApi.AdminClient(system);

        HttpResponseMessage response = await OrderingApi.PostAsync(admin, $"/api/v1/orders/{orderId}/deliver");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        JsonElement order = await OrderingApi.GetOrderAsync(admin, orderId);
        Assert.Equal("Delivered", order.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, order.GetProperty("updatedOnUtc").ValueKind);
    }

    [Theory]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task DeliverOrder_NotShipped_IsAConflict_AndLeavesTheOrderAlone(OrderStatus status)
    {
        long orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), status);
        using HttpClient admin = OrderingApi.AdminClient(system);

        HttpResponseMessage response = await OrderingApi.PostAsync(admin, $"/api/v1/orders/{orderId}/deliver");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(status.ToString(), await OrderingApi.GetStatusAsync(admin, orderId));
    }

    [Fact]
    public async Task DeliverOrder_UnknownOrder_GetsNotFound()
    {
        using HttpClient admin = OrderingApi.AdminClient(system);

        HttpResponseMessage response = await OrderingApi.PostAsync(admin, $"/api/v1/orders/{long.MaxValue}/deliver");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShipAndDeliver_NonAdmin_IsForbidden()
    {
        long userId = TestJwt.NewUserId();
        long orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, userId, OrderStatus.Shipped);
        using HttpClient customer = OrderingApi.CustomerClient(system, userId);
        using HttpClient admin = OrderingApi.AdminClient(system);

        Assert.Equal(HttpStatusCode.Forbidden, (await OrderingApi.PostAsync(customer, $"/api/v1/orders/{orderId}/ship")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await OrderingApi.PostAsync(customer, $"/api/v1/orders/{orderId}/deliver")).StatusCode);
        Assert.Equal("Shipped", await OrderingApi.GetStatusAsync(admin, orderId));
    }
}
