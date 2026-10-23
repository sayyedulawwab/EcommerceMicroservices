using EcommerceMicroservices.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests.Cart;

// Clearing the cart after payment (OrderStatusChangedToPaidIntegrationEventHandler) is covered end to end by OrderSagaTests.
[Collection(SystemCollection.Name)]
public sealed class CartApiTests(SystemFixture system)
{
    private const string CartUrl = "/api/v1/carts";
    private const string ItemsUrl = "/api/v1/carts/items";

    [Fact]
    public async Task GetCart_WithoutStoredCart_ReturnsEmptyCart()
    {
        using HttpClient client = CreateCustomer(out long userId);

        JsonElement cart = await client.GetFromJsonAsync<JsonElement>(CartUrl, Ct);

        Assert.Equal(Guid.Empty, cart.GetProperty("id").GetGuid());
        Assert.Equal(userId, cart.GetProperty("userId").GetInt64());
        Assert.Equal(0, cart.GetProperty("cartItems").GetArrayLength());
    }

    [Fact]
    public async Task AddItem_WithoutCart_CreatesCart()
    {
        using HttpClient client = CreateCustomer(out long userId);

        HttpResponseMessage response = await client.PostAsJsonAsync(ItemsUrl, Item(productId: 1, quantity: 2, price: 10m), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement cart = await client.GetFromJsonAsync<JsonElement>(CartUrl, Ct);
        Assert.NotEqual(Guid.Empty, cart.GetProperty("id").GetGuid());
        Assert.Equal(userId, cart.GetProperty("userId").GetInt64());
        JsonElement item = Assert.Single([.. cart.GetProperty("cartItems").EnumerateArray()]);
        Assert.Equal(1, item.GetProperty("productId").GetInt64());
        Assert.Equal(2, item.GetProperty("quantity").GetInt32());
        Assert.Equal(20m, cart.GetProperty("totalPriceAmount").GetDecimal());
        Assert.Equal("BDT", cart.GetProperty("totalPriceCurrency").GetString());
    }

    [Fact]
    public async Task AddItem_ToExistingCart_AddsItemsAndMergesSameProduct()
    {
        using HttpClient client = CreateCustomer(out _);
        await AddAsync(client, Item(productId: 1, quantity: 2, price: 10m));

        await AddAsync(client, Item(productId: 2, quantity: 1, price: 5m));
        JsonElement cart = await AddAsync(client, Item(productId: 1, quantity: 3, price: 10m));

        Assert.Equal(2, cart.GetProperty("cartItems").GetArrayLength());
        Assert.Equal(5, QuantityOf(cart, productId: 1));
        Assert.Equal(55m, cart.GetProperty("totalPriceAmount").GetDecimal());
    }

    [Fact]
    public async Task AddItem_InDifferentCurrency_IsRejectedAndCartUnchanged()
    {
        using HttpClient client = CreateCustomer(out _);
        await AddAsync(client, Item(productId: 1, quantity: 1, price: 10m, currency: "BDT"));

        HttpResponseMessage response = await client.PostAsJsonAsync(ItemsUrl, Item(productId: 2, quantity: 1, price: 10m, currency: "USD"), Ct);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "Cart.MixedCurrencies");
        JsonElement cart = await client.GetFromJsonAsync<JsonElement>(CartUrl, Ct);
        Assert.Equal(1, cart.GetProperty("cartItems").GetArrayLength());
    }

    [Fact]
    public async Task AddItem_AboveMaxQuantityInTotal_IsRejectedAndCartUnchanged()
    {
        using HttpClient client = CreateCustomer(out _);
        await AddAsync(client, Item(productId: 1, quantity: 600, price: 1m));

        HttpResponseMessage response = await client.PostAsJsonAsync(ItemsUrl, Item(productId: 1, quantity: 600, price: 1m), Ct);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "Cart.InvalidQuantity");
        JsonElement cart = await client.GetFromJsonAsync<JsonElement>(CartUrl, Ct);
        Assert.Equal(600, QuantityOf(cart, productId: 1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public async Task AddItem_WithQuantityInRange_IsAccepted(int quantity)
    {
        using HttpClient client = CreateCustomer(out _);

        HttpResponseMessage response = await client.PostAsJsonAsync(ItemsUrl, Item(productId: 1, quantity: quantity, price: 1m), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 1, "BDT", "Widget", "Quantity")]
    [InlineData(1001, 1, "BDT", "Widget", "Quantity")]
    [InlineData(1, 0, "BDT", "Widget", "PriceAmount")]
    [InlineData(1, -1, "BDT", "Widget", "PriceAmount")]
    [InlineData(1, 1, "EUR", "Widget", "PriceCurrency")]
    [InlineData(1, 1, "BDT", "", "ProductName")]
    public async Task AddItem_WithInvalidItem_ReturnsValidationError(int quantity, decimal price, string currency, string name, string property)
    {
        using HttpClient client = CreateCustomer(out _);

        HttpResponseMessage response = await client.PostAsJsonAsync(ItemsUrl, Item(productId: 1, quantity: quantity, price: price, currency: currency, name: name), Ct);

        string[] properties = await AssertValidationErrorAsync(response);
        Assert.Contains(property, properties);
    }

    [Fact]
    public async Task UpdateItemQuantity_SetsQuantity()
    {
        using HttpClient client = CreateCustomer(out _);
        await AddAsync(client, Item(productId: 1, quantity: 2, price: 10m));

        HttpResponseMessage response = await client.PutAsJsonAsync($"{ItemsUrl}/1", new { quantity = 7 }, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement cart = await client.GetFromJsonAsync<JsonElement>(CartUrl, Ct);
        Assert.Equal(7, QuantityOf(cart, productId: 1));
        Assert.Equal(70m, cart.GetProperty("totalPriceAmount").GetDecimal());
    }

    [Fact]
    public async Task UpdateItemQuantity_ToZero_RemovesItem()
    {
        using HttpClient client = CreateCustomer(out _);
        await AddAsync(client, Item(productId: 1, quantity: 2, price: 10m));
        await AddAsync(client, Item(productId: 2, quantity: 1, price: 5m));

        HttpResponseMessage response = await client.PutAsJsonAsync($"{ItemsUrl}/1", new { quantity = 0 }, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement cart = await client.GetFromJsonAsync<JsonElement>(CartUrl, Ct);
        JsonElement item = Assert.Single([.. cart.GetProperty("cartItems").EnumerateArray()]);
        Assert.Equal(2, item.GetProperty("productId").GetInt64());
    }

    [Fact]
    public async Task UpdateItemQuantity_WithoutCart_ReturnsItemNotFound()
    {
        using HttpClient client = CreateCustomer(out _);

        HttpResponseMessage response = await client.PutAsJsonAsync($"{ItemsUrl}/1", new { quantity = 3 }, Ct);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Cart.ItemNotFound");
    }

    [Fact]
    public async Task UpdateItemQuantity_ForProductNotInCart_ReturnsItemNotFoundAndCartUnchanged()
    {
        using HttpClient client = CreateCustomer(out _);
        await AddAsync(client, Item(productId: 1, quantity: 2, price: 10m));

        HttpResponseMessage response = await client.PutAsJsonAsync($"{ItemsUrl}/99", new { quantity = 3 }, Ct);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Cart.ItemNotFound");
        JsonElement cart = await client.GetFromJsonAsync<JsonElement>(CartUrl, Ct);
        Assert.Equal(2, QuantityOf(cart, productId: 1));
    }

    [Fact]
    public async Task RemoveItem_RemovesIt()
    {
        using HttpClient client = CreateCustomer(out _);
        await AddAsync(client, Item(productId: 1, quantity: 2, price: 10m));
        await AddAsync(client, Item(productId: 2, quantity: 1, price: 5m));

        HttpResponseMessage response = await client.DeleteAsync($"{ItemsUrl}/1", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement cart = await client.GetFromJsonAsync<JsonElement>(CartUrl, Ct);
        JsonElement item = Assert.Single([.. cart.GetProperty("cartItems").EnumerateArray()]);
        Assert.Equal(2, item.GetProperty("productId").GetInt64());
        Assert.Equal(5m, cart.GetProperty("totalPriceAmount").GetDecimal());
    }

    [Fact]
    public async Task RemoveItem_WithoutCart_ReturnsItemNotFound()
    {
        using HttpClient client = CreateCustomer(out _);

        HttpResponseMessage response = await client.DeleteAsync($"{ItemsUrl}/1", Ct);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Cart.ItemNotFound");
    }

    [Fact]
    public async Task RemoveItem_ForProductNotInCart_ReturnsItemNotFoundAndCartUnchanged()
    {
        using HttpClient client = CreateCustomer(out _);
        await AddAsync(client, Item(productId: 1, quantity: 2, price: 10m));

        HttpResponseMessage response = await client.DeleteAsync($"{ItemsUrl}/99", Ct);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Cart.ItemNotFound");
        JsonElement cart = await client.GetFromJsonAsync<JsonElement>(CartUrl, Ct);
        Assert.Equal(1, cart.GetProperty("cartItems").GetArrayLength());
    }

    [Fact]
    public async Task ClearCart_RemovesStoredCart()
    {
        using HttpClient client = CreateCustomer(out _);
        await AddAsync(client, Item(productId: 1, quantity: 2, price: 10m));

        HttpResponseMessage response = await client.DeleteAsync(CartUrl, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        JsonElement cart = await client.GetFromJsonAsync<JsonElement>(CartUrl, Ct);
        Assert.Equal(Guid.Empty, cart.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task ReplaceCart_WithItems_StoresNewCartWithMergedItems()
    {
        using HttpClient client = CreateCustomer(out _);
        await AddAsync(client, Item(productId: 9, quantity: 1, price: 1m));

        HttpResponseMessage response = await client.PutAsJsonAsync(
            CartUrl,
            new { cartItems = new[] { Item(productId: 1, quantity: 2, price: 10m), Item(productId: 1, quantity: 1, price: 10m), Item(productId: 2, quantity: 1, price: 5m) } },
            Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement cart = await client.GetFromJsonAsync<JsonElement>(CartUrl, Ct);
        Assert.Equal(2, cart.GetProperty("cartItems").GetArrayLength());
        Assert.Equal(3, QuantityOf(cart, productId: 1));
        Assert.Equal(35m, cart.GetProperty("totalPriceAmount").GetDecimal());
    }

    [Fact]
    public async Task ReplaceCart_WithEmptyList_RemovesCart()
    {
        using HttpClient client = CreateCustomer(out _);
        await AddAsync(client, Item(productId: 1, quantity: 2, price: 10m));

        HttpResponseMessage response = await client.PutAsJsonAsync(CartUrl, new { cartItems = Array.Empty<object>() }, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement cart = await client.GetFromJsonAsync<JsonElement>(CartUrl, Ct);
        Assert.Equal(Guid.Empty, cart.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task ReplaceCart_WithMixedCurrencies_IsRejectedAndCartUnchanged()
    {
        using HttpClient client = CreateCustomer(out _);
        await AddAsync(client, Item(productId: 9, quantity: 1, price: 1m));

        HttpResponseMessage response = await client.PutAsJsonAsync(
            CartUrl,
            new { cartItems = new[] { Item(productId: 1, quantity: 1, price: 10m, currency: "BDT"), Item(productId: 2, quantity: 1, price: 10m, currency: "USD") } },
            Ct);

        string[] properties = await AssertValidationErrorAsync(response);
        Assert.Contains("CartItems", properties);
        JsonElement cart = await client.GetFromJsonAsync<JsonElement>(CartUrl, Ct);
        Assert.Equal(9, cart.GetProperty("cartItems")[0].GetProperty("productId").GetInt64());
    }

    [Fact]
    public async Task ReplaceCart_WithInvalidItem_ReportsTheItemProperty()
    {
        using HttpClient client = CreateCustomer(out _);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            CartUrl,
            new { cartItems = new[] { Item(productId: 1, quantity: 1, price: 10m), Item(productId: 2, quantity: 0, price: 10m) } },
            Ct);

        string[] properties = await AssertValidationErrorAsync(response);
        Assert.Contains("CartItems[1].Quantity", properties);
    }

    [Fact]
    public async Task ReplaceCart_WithMoreThanMaxItems_ReturnsValidationError()
    {
        using HttpClient client = CreateCustomer(out _);
        object[] items = [.. Enumerable.Range(1, 101).Select(productId => Item(productId, quantity: 1, price: 1m))];

        HttpResponseMessage response = await client.PutAsJsonAsync(CartUrl, new { cartItems = items }, Ct);

        string[] properties = await AssertValidationErrorAsync(response);
        Assert.Contains("CartItems", properties);
    }

    private HttpClient CreateCustomer(out long userId)
    {
        userId = TestJwt.NewUserId();

        return SystemFixture.CreateClient(system.Cart, TestJwt.Create(userId));
    }

    private static object Item(long productId, int quantity, decimal price, string currency = "BDT", string name = "Widget")
    {
        return new { productId, productName = name, priceAmount = price, priceCurrency = currency, quantity };
    }

    private static async Task<JsonElement> AddAsync(HttpClient client, object item)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(ItemsUrl, item, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

    private static int QuantityOf(JsonElement cart, long productId)
    {
        return cart.GetProperty("cartItems").EnumerateArray()
            .Single(item => item.GetProperty("productId").GetInt64() == productId)
            .GetProperty("quantity").GetInt32();
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string errorName)
    {
        Assert.Equal(status, response.StatusCode);
        JsonElement error = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(errorName, error.GetProperty("name").GetString());
    }

    private static async Task<string[]> AssertValidationErrorAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement error = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Validation.General", error.GetProperty("name").GetString());

        return [.. error.GetProperty("errors").EnumerateArray().Select(e => e.GetProperty("name").GetString()!)];
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
