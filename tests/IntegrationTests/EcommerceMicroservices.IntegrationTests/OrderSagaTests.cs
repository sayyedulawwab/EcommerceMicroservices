using EcommerceMicroservices.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests;

/// <summary>
/// The order saga end to end: Ordering → Catalog (reserve stock, price) → Ordering → Payment → Ordering → Catalog/Cart,
/// over the real RabbitMQ broker and the transactional outbox.
/// </summary>
[Collection(SystemCollection.Name)]
public sealed class OrderSagaTests(SystemFixture system)
{
    private static readonly string[] FinalStatuses = ["Paid", "Cancelled", "Shipped", "Delivered"];

    [Fact]
    public async Task PlacedOrder_IsPricedByCatalog_Paid_StockCommitted_AndCartCleared()
    {
        using HttpClient admin = SystemFixture.CreateClient(system.Catalog, TestJwt.Create(TestJwt.NewUserId(), isAdmin: true));
        long productId = await CatalogApi.CreateProductAsync(admin, price: 100, quantity: 10);

        long userId = TestJwt.NewUserId();
        string userToken = TestJwt.Create(userId);
        using HttpClient cart = SystemFixture.CreateClient(system.Cart, userToken);
        HttpResponseMessage addToCart = await cart.PostAsJsonAsync(
            "/api/v1/carts/items",
            new { productId, productName = "From the product page", priceAmount = 100m, priceCurrency = "BDT", quantity = 2 },
            Ct);
        Assert.Equal(HttpStatusCode.OK, addToCart.StatusCode);

        using HttpClient ordering = SystemFixture.CreateClient(system.Ordering, userToken);
        long orderId = await PlaceOrderAsync(ordering, productId, quantity: 2);

        JsonElement order = await WaitForFinalStatusAsync(ordering, orderId);

        Assert.Equal("Paid", order.GetProperty("status").GetString());
        Assert.Equal(200m, order.GetProperty("totalPriceAmount").GetDecimal());
        Assert.Equal("BDT", order.GetProperty("totalPriceCurrency").GetString());
        JsonElement item = Assert.Single(order.GetProperty("orderItems").EnumerateArray());
        Assert.StartsWith("Product ", item.GetProperty("productName").GetString(), StringComparison.Ordinal);
        Assert.Equal(100m, item.GetProperty("productPriceAmount").GetDecimal());

        Assert.Equal(8, await CatalogApi.GetStockAsync(admin, productId));

        await Eventually.UntilAsync(
            async () => (await cart.GetFromJsonAsync<JsonElement>("/api/v1/carts", Ct)).GetProperty("cartItems").GetArrayLength(),
            itemCount => itemCount == 0,
            "the cart to be cleared after payment");
    }

    [Fact]
    public async Task OrderForMoreThanTheStock_IsCancelled_AndStockIsUntouched()
    {
        using HttpClient admin = SystemFixture.CreateClient(system.Catalog, TestJwt.Create(TestJwt.NewUserId(), isAdmin: true));
        long productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 3);

        using HttpClient ordering = SystemFixture.CreateClient(system.Ordering, TestJwt.Create(TestJwt.NewUserId()));
        long orderId = await PlaceOrderAsync(ordering, productId, quantity: 4);

        JsonElement order = await WaitForFinalStatusAsync(ordering, orderId);

        Assert.Equal("Cancelled", order.GetProperty("status").GetString());
        Assert.Contains("Stock rejected", order.GetProperty("cancellationReason").GetString(), StringComparison.Ordinal);
        Assert.Equal(3, await CatalogApi.GetStockAsync(admin, productId));
    }

    [Fact]
    public async Task FailedPayment_CancelsTheOrder_AndReleasesTheReservedStock()
    {
        using HttpClient admin = SystemFixture.CreateClient(system.Catalog, TestJwt.Create(TestJwt.NewUserId(), isAdmin: true));
        long productId = await CatalogApi.CreateProductAsync(admin, price: SystemFixture.PaymentLimit, quantity: 5);

        using HttpClient ordering = SystemFixture.CreateClient(system.Ordering, TestJwt.Create(TestJwt.NewUserId()));
        long orderId = await PlaceOrderAsync(ordering, productId, quantity: 2);

        JsonElement order = await WaitForFinalStatusAsync(ordering, orderId);

        Assert.Equal("Cancelled", order.GetProperty("status").GetString());
        Assert.Contains("Payment failed", order.GetProperty("cancellationReason").GetString(), StringComparison.Ordinal);

        await Eventually.UntilAsync(
            () => CatalogApi.GetStockAsync(admin, productId),
            stock => stock == 5,
            "the reserved stock to be released");
    }

    [Fact]
    public async Task PaidOrder_CanBeShippedAndDeliveredByAnAdmin_ButNotCancelledByTheCustomer()
    {
        using HttpClient catalogAdmin = SystemFixture.CreateClient(system.Catalog, TestJwt.Create(TestJwt.NewUserId(), isAdmin: true));
        long productId = await CatalogApi.CreateProductAsync(catalogAdmin, price: 25, quantity: 5);

        using HttpClient customer = SystemFixture.CreateClient(system.Ordering, TestJwt.Create(TestJwt.NewUserId()));
        long orderId = await PlaceOrderAsync(customer, productId, quantity: 1);
        Assert.Equal("Paid", (await WaitForFinalStatusAsync(customer, orderId)).GetProperty("status").GetString());

        HttpResponseMessage customerCancel = await PostAsync(customer, $"/api/v1/orders/{orderId}/cancel");
        HttpResponseMessage customerShip = await PostAsync(customer, $"/api/v1/orders/{orderId}/ship");

        Assert.Equal(HttpStatusCode.Conflict, customerCancel.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, customerShip.StatusCode);

        using HttpClient orderingAdmin = SystemFixture.CreateClient(system.Ordering, TestJwt.Create(TestJwt.NewUserId(), isAdmin: true));

        Assert.Equal(HttpStatusCode.NoContent, (await PostAsync(orderingAdmin, $"/api/v1/orders/{orderId}/ship")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsync(orderingAdmin, $"/api/v1/orders/{orderId}/deliver")).StatusCode);

        JsonElement order = await customer.GetFromJsonAsync<JsonElement>($"/api/v1/orders/{orderId}", Ct);
        Assert.Equal("Delivered", order.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Customers_OnlySeeTheirOwnOrders()
    {
        using HttpClient catalogAdmin = SystemFixture.CreateClient(system.Catalog, TestJwt.Create(TestJwt.NewUserId(), isAdmin: true));
        long productId = await CatalogApi.CreateProductAsync(catalogAdmin, price: 5, quantity: 5);

        using HttpClient owner = SystemFixture.CreateClient(system.Ordering, TestJwt.Create(TestJwt.NewUserId()));
        using HttpClient stranger = SystemFixture.CreateClient(system.Ordering, TestJwt.Create(TestJwt.NewUserId()));
        long orderId = await PlaceOrderAsync(owner, productId, quantity: 1);

        HttpResponseMessage strangerView = await stranger.GetAsync($"/api/v1/orders/{orderId}", Ct);
        JsonElement strangerOrders = await stranger.GetFromJsonAsync<JsonElement>("/api/v1/orders", Ct);
        JsonElement ownerOrders = await owner.GetFromJsonAsync<JsonElement>("/api/v1/orders", Ct);

        Assert.Equal(HttpStatusCode.NotFound, strangerView.StatusCode);
        Assert.Equal(0, strangerOrders.GetArrayLength());
        Assert.Contains(ownerOrders.EnumerateArray(), o => o.GetProperty("id").GetInt64() == orderId);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<long> PlaceOrderAsync(HttpClient ordering, long productId, int quantity)
    {
        HttpResponseMessage response = await ordering.PostAsJsonAsync(
            "/api/v1/orders",
            new { orderItems = new[] { new { productId, quantity } } },
            Ct);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        return await CatalogApi.ReadIdAsync(response);
    }

    private static Task<JsonElement> WaitForFinalStatusAsync(HttpClient ordering, long orderId)
    {
        return Eventually.UntilAsync(
            () => ordering.GetFromJsonAsync<JsonElement>($"/api/v1/orders/{orderId}", Ct),
            order => FinalStatuses.Contains(order.GetProperty("status").GetString()),
            $"order {orderId} to be paid or cancelled");
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string url)
    {
        return client.PostAsync(url, content: null, Ct);
    }
}
