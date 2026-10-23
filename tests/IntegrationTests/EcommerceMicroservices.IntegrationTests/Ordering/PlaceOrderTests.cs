using EcommerceMicroservices.IntegrationTests.Infrastructure;
using SharedKernel.Events;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests.Ordering;

/// <summary>Placing an order: validation, what is stored, and the event that starts the saga.</summary>
[Collection(SystemCollection.Name)]
public sealed class PlaceOrderTests(SystemFixture system)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PlaceOrder_ValidRequest_StoresTheOrderForTheCaller_AndPublishesOrderPlacedWithMergedItems()
    {
        Guid userId = TestJwt.NewUserId();
        using HttpClient customer = OrderingApi.CustomerClient(system, userId);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderPlacedIntegrationEvent> placed = await BusProbe<OrderPlacedIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        // The same product listed twice becomes one item with the combined quantity.
        HttpResponseMessage response = await OrderingApi.PlaceAsync(
            customer,
            (OrderingTestData.KeyboardId, 1),
            (OrderingTestData.MouseId, 4),
            (OrderingTestData.KeyboardId, 2));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Guid orderId = await OrderingApi.ReadIdAsync(response);

        OrderPlacedIntegrationEvent @event = await placed.WaitForAsync(e => e.OrderId == orderId, "OrderPlaced to be published");
        Assert.Equal(userId, @event.UserId);
        Assert.Equal(2, @event.OrderStockItems.Count);
        Assert.Equal(3, Assert.Single(@event.OrderStockItems, item => item.ProductId == OrderingTestData.KeyboardId).Quantity);
        Assert.Equal(4, Assert.Single(@event.OrderStockItems, item => item.ProductId == OrderingTestData.MouseId).Quantity);

        JsonElement order = await OrderingApi.GetOrderAsync(admin, orderId);
        Assert.Equal(userId, order.GetProperty("userId").GetGuid());
        Assert.Equal(2, order.GetProperty("orderItems").GetArrayLength());
        Assert.Contains(
            order.GetProperty("orderItems").EnumerateArray(),
            item => item.GetProperty("productId").GetGuid() == OrderingTestData.KeyboardId && item.GetProperty("quantity").GetInt32() == 3);
    }

    [Fact]
    public async Task PlaceOrder_MaximumQuantity_IsAccepted()
    {
        using HttpClient customer = OrderingApi.CustomerClient(system, TestJwt.NewUserId());

        HttpResponseMessage response = await OrderingApi.PlaceAsync(customer, (OrderingTestData.KeyboardId, 1000));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_WithoutAToken_IsUnauthorized()
    {
        using HttpClient anonymous = SystemFixture.CreateClient(system.Ordering);

        HttpResponseMessage response = await OrderingApi.PlaceAsync(anonymous, (OrderingTestData.KeyboardId, 1));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_NoItems_IsRejected_AndNothingIsStored()
    {
        using HttpClient customer = OrderingApi.CustomerClient(system, TestJwt.NewUserId());

        HttpResponseMessage response = await OrderingApi.PlaceAsync(customer);

        await AssertRejectedAsync(customer, response);
    }

    [Fact]
    public async Task PlaceOrder_ItemsMissingFromTheBody_IsRejected_AndNothingIsStored()
    {
        using HttpClient customer = OrderingApi.CustomerClient(system, TestJwt.NewUserId());

        HttpResponseMessage response = await customer.PostAsJsonAsync("/api/v1/orders", new { }, Ct);

        await AssertRejectedAsync(customer, response);
    }

    [Fact]
    public async Task PlaceOrder_MoreThanTheMaximumNumberOfItems_IsRejected_AndNothingIsStored()
    {
        using HttpClient customer = OrderingApi.CustomerClient(system, TestJwt.NewUserId());
        (Guid ProductId, int Quantity)[] items = [.. Enumerable.Range(1, 101).Select(_ => (Guid.CreateVersion7(), 1))];

        HttpResponseMessage response = await OrderingApi.PlaceAsync(customer, items);

        await AssertRejectedAsync(customer, response);
    }

    [Fact]
    public async Task PlaceOrder_EmptyProductId_IsRejected_AndNothingIsStored()
    {
        using HttpClient customer = OrderingApi.CustomerClient(system, TestJwt.NewUserId());

        HttpResponseMessage response = await OrderingApi.PlaceAsync(customer, (Guid.Empty, 1));

        await AssertRejectedAsync(customer, response);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public async Task PlaceOrder_QuantityOutOfRange_IsRejected_AndNothingIsStored(int quantity)
    {
        using HttpClient customer = OrderingApi.CustomerClient(system, TestJwt.NewUserId());

        HttpResponseMessage response = await OrderingApi.PlaceAsync(customer, (OrderingTestData.KeyboardId, quantity));

        await AssertRejectedAsync(customer, response);
    }

    [Fact]
    public async Task PlaceOrder_EmptyUserId_IsRejected()
    {
        using HttpClient customer = OrderingApi.CustomerClient(system, Guid.Empty);

        HttpResponseMessage response = await OrderingApi.PlaceAsync(customer, (OrderingTestData.KeyboardId, 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task AssertRejectedAsync(HttpClient customer, HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        JsonElement orders = await customer.GetFromJsonAsync<JsonElement>("/api/v1/orders", Ct);
        Assert.Equal(0, orders.GetArrayLength());
    }
}
