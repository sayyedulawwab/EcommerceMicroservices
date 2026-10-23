using EcommerceMicroservices.IntegrationTests.Infrastructure;
using Ordering.Domain.Orders;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests.Ordering;

/// <summary>Reading orders: ownership, admin access, and the shape of the response.</summary>
[Collection(SystemCollection.Name)]
public sealed class GetOrderTests(SystemFixture system)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetOrder_Owner_SeesTheOrderWithItsPricedItems()
    {
        long userId = TestJwt.NewUserId();
        long orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, userId, OrderStatus.Paid);
        using HttpClient owner = OrderingApi.CustomerClient(system, userId);

        HttpResponseMessage response = await owner.GetAsync($"/api/v1/orders/{orderId}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement order = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(orderId, order.GetProperty("id").GetInt64());
        Assert.Equal(userId, order.GetProperty("userId").GetInt64());
        Assert.Equal("Paid", order.GetProperty("status").GetString());
        Assert.Equal(35m, order.GetProperty("totalPriceAmount").GetDecimal());
        Assert.Equal("USD", order.GetProperty("totalPriceCurrency").GetString());
        Assert.Equal(JsonValueKind.Null, order.GetProperty("cancellationReason").ValueKind);
        Assert.Equal(2, order.GetProperty("orderItems").GetArrayLength());
        JsonElement keyboard = Assert.Single(
            order.GetProperty("orderItems").EnumerateArray(),
            item => item.GetProperty("productId").GetInt64() == OrderingTestData.KeyboardId);
        Assert.Equal(orderId, keyboard.GetProperty("orderId").GetInt64());
        Assert.Equal("Keyboard", keyboard.GetProperty("productName").GetString());
        Assert.Equal(10m, keyboard.GetProperty("productPriceAmount").GetDecimal());
        Assert.Equal("USD", keyboard.GetProperty("productPriceCurrency").GetString());
        Assert.Equal(2, keyboard.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task GetOrder_CancelledOrder_ShowsTheCancellationReason()
    {
        long userId = TestJwt.NewUserId();
        long orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, userId, OrderStatus.Cancelled);
        using HttpClient owner = OrderingApi.CustomerClient(system, userId);

        JsonElement order = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/orders/{orderId}", Ct);

        Assert.Equal("Cancelled", order.GetProperty("status").GetString());
        Assert.Equal(OrderingTestData.SeededCancellationReason, order.GetProperty("cancellationReason").GetString());
        Assert.NotEqual(JsonValueKind.Null, order.GetProperty("updatedOnUtc").ValueKind);
    }

    [Fact]
    public async Task GetOrder_AnotherNonAdminUser_GetsNotFound()
    {
        long orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), OrderStatus.Paid);
        using HttpClient stranger = OrderingApi.CustomerClient(system, TestJwt.NewUserId());

        HttpResponseMessage response = await stranger.GetAsync($"/api/v1/orders/{orderId}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_Admin_SeesAnyUsersOrder()
    {
        long userId = TestJwt.NewUserId();
        long orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, userId, OrderStatus.Paid);
        using HttpClient admin = OrderingApi.AdminClient(system);

        HttpResponseMessage response = await admin.GetAsync($"/api/v1/orders/{orderId}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement order = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(userId, order.GetProperty("userId").GetInt64());
    }

    [Fact]
    public async Task GetOrder_UnknownOrder_GetsNotFound_EvenForAnAdmin()
    {
        using HttpClient customer = OrderingApi.CustomerClient(system, TestJwt.NewUserId());
        using HttpClient admin = OrderingApi.AdminClient(system);

        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync($"/api/v1/orders/{long.MaxValue}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/orders/{long.MaxValue}", Ct)).StatusCode);
    }

    [Fact]
    public async Task GetOrder_WithoutAToken_IsUnauthorized()
    {
        using HttpClient anonymous = SystemFixture.CreateClient(system.Ordering);

        HttpResponseMessage response = await anonymous.GetAsync("/api/v1/orders/1", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyOrders_ReturnsOnlyTheCallersOrders_NewestFirst()
    {
        long userId = TestJwt.NewUserId();
        long older = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, userId, OrderStatus.Paid);
        long newer = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, userId, OrderStatus.Placed);
        long someoneElses = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), OrderStatus.Paid);
        using HttpClient customer = OrderingApi.CustomerClient(system, userId);

        JsonElement orders = await customer.GetFromJsonAsync<JsonElement>("/api/v1/orders", Ct);

        long[] ids = [.. orders.EnumerateArray().Select(order => order.GetProperty("id").GetInt64())];
        Assert.Equal([newer, older], ids);
        Assert.DoesNotContain(someoneElses, ids);
        Assert.All(orders.EnumerateArray(), order => Assert.Equal(2, order.GetProperty("orderItems").GetArrayLength()));
    }

    [Fact]
    public async Task GetAllOrders_Admin_SeesEveryUsersOrders_NewestFirst()
    {
        long older = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), OrderStatus.Paid);
        long newer = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), OrderStatus.Placed);
        using HttpClient admin = OrderingApi.AdminClient(system);

        HttpResponseMessage response = await admin.GetAsync("/api/v1/orders/all", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement orders = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        List<long> ids = [.. orders.EnumerateArray().Select(order => order.GetProperty("id").GetInt64())];
        Assert.Contains(older, ids);
        Assert.Contains(newer, ids);
        Assert.True(ids.IndexOf(newer) < ids.IndexOf(older), "Orders should be listed newest first.");
        Assert.All(orders.EnumerateArray(), order => Assert.True(order.GetProperty("orderItems").GetArrayLength() > 0));
    }

    [Fact]
    public async Task GetAllOrders_NonAdmin_IsForbidden()
    {
        using HttpClient customer = OrderingApi.CustomerClient(system, TestJwt.NewUserId());

        HttpResponseMessage response = await customer.GetAsync("/api/v1/orders/all", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
