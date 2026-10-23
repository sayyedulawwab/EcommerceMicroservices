using EcommerceMicroservices.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests.Catalog;

[Collection(SystemCollection.Name)]
public sealed class ProductApiTests(SystemFixture system)
{
    [Fact]
    public async Task AddProduct_WithExistingCategory_StoresIt()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);

        HttpResponseMessage response = await admin.PostAsJsonAsync(
            "/api/v1/products",
            new { name = "Widget", description = "A widget", priceCurrency = "USD", priceAmount = 12.5m, quantity = 7, categoryId },
            Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        JsonElement product = await GetProductAsync(admin, await CatalogApi.ReadIdAsync(response));
        Assert.Equal("Widget", product.GetProperty("name").GetString());
        Assert.Equal("A widget", product.GetProperty("description").GetString());
        Assert.Equal("USD", product.GetProperty("priceCurrency").GetString());
        Assert.Equal(12.5m, product.GetProperty("priceAmount").GetDecimal());
        Assert.Equal(7, product.GetProperty("quantity").GetInt32());
        Assert.Equal(categoryId, product.GetProperty("categoryId").GetInt64());
    }

    [Fact]
    public async Task AddProduct_WithUnknownCategory_IsRejected_AndAddsNothing()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        string name = $"Orphan {Guid.NewGuid():N}";

        HttpResponseMessage response = await admin.PostAsJsonAsync(
            "/api/v1/products",
            new { name, description = "x", priceCurrency = "BDT", priceAmount = 1m, quantity = 1, categoryId = long.MaxValue },
            Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Product.CategoryNotFound", await CatalogTestSupport.ReadErrorNameAsync(response));
        Assert.Empty(await SearchAsync(admin, $"keyword={name}"));
    }

    [Theory]
    [InlineData("EUR", 1, 1)]
    [InlineData("usd", 1, 1)]
    [InlineData("", 1, 1)]
    [InlineData("BDT", 0, 1)]
    [InlineData("BDT", -1, 1)]
    [InlineData("BDT", 1, -1)]
    public async Task AddProduct_WithInvalidData_IsRejected(string currency, int price, int quantity)
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);

        HttpResponseMessage response = await admin.PostAsJsonAsync(
            "/api/v1/products",
            new { name = "Invalid", description = "x", priceCurrency = currency, priceAmount = price, quantity, categoryId },
            Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("BDT")]
    public async Task AddProduct_WithASupportedCurrency_IsAccepted(string currency)
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);

        long productId = await CatalogApi.CreateProductAsync(admin, price: 1, quantity: 1, currency: currency);

        Assert.Equal(currency, (await GetProductAsync(admin, productId)).GetProperty("priceCurrency").GetString());
    }

    [Fact]
    public async Task GetProduct_Unknown_IsNotFound()
    {
        using HttpClient anonymous = SystemFixture.CreateClient(system.Catalog);

        HttpResponseMessage response = await anonymous.GetAsync($"/api/v1/products/{long.MaxValue}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EditProduct_UpdatesItsFields()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5);
        long newCategoryId = await CatalogApi.CreateCategoryAsync(admin);

        HttpResponseMessage response = await EditAsync(admin, productId, newCategoryId, name: "Renamed", price: 20, quantity: 9);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement product = await GetProductAsync(admin, productId);
        Assert.Equal("Renamed", product.GetProperty("name").GetString());
        Assert.Equal(20m, product.GetProperty("priceAmount").GetDecimal());
        Assert.Equal(9, product.GetProperty("quantity").GetInt32());
        Assert.Equal(newCategoryId, product.GetProperty("categoryId").GetInt64());
        Assert.NotEqual(JsonValueKind.Null, product.GetProperty("updatedOnUtc").ValueKind);
    }

    [Fact]
    public async Task EditProduct_KeepingItsCategory_UpdatesIt()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);
        long productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5, categoryId: categoryId);

        HttpResponseMessage response = await EditAsync(admin, productId, categoryId, name: "Same category", price: 11, quantity: 6);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(6, await CatalogApi.GetStockAsync(admin, productId));
    }

    [Fact]
    public async Task EditProduct_Unknown_IsNotFound()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);

        HttpResponseMessage response = await EditAsync(admin, long.MaxValue, categoryId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EditProduct_ToAnUnknownCategory_IsRejected_AndKeepsTheProduct()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);
        long productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5, categoryId: categoryId);

        HttpResponseMessage response = await EditAsync(admin, productId, long.MaxValue, name: "Should not stick");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Product.CategoryNotFound", await CatalogTestSupport.ReadErrorNameAsync(response));
        JsonElement product = await GetProductAsync(admin, productId);
        Assert.NotEqual("Should not stick", product.GetProperty("name").GetString());
        Assert.Equal(categoryId, product.GetProperty("categoryId").GetInt64());
    }

    [Fact]
    public async Task DeleteProduct_RemovesIt()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5);

        HttpResponseMessage response = await admin.DeleteAsync($"/api/v1/products/{productId}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/products/{productId}", Ct)).StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_Unknown_IsNotFound()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);

        HttpResponseMessage response = await admin.DeleteAsync($"/api/v1/products/{long.MaxValue}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EditAndDeleteProduct_RequireTheAdminRole()
    {
        using HttpClient customer = SystemFixture.CreateClient(system.Catalog, TestJwt.Create(TestJwt.NewUserId()));

        HttpResponseMessage edit = await EditAsync(customer, 1, 1);
        HttpResponseMessage delete = await customer.DeleteAsync("/api/v1/products/1", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Search_KeywordInName_MatchesTheProduct()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        string keyword = $"Name{Guid.NewGuid():N}";
        long match = await CatalogApi.CreateProductAsync(admin, price: 1, quantity: 1, name: $"Big {keyword} widget");
        await CatalogApi.CreateProductAsync(admin, price: 1, quantity: 1);

        JsonElement[] items = await SearchAsync(admin, $"keyword={keyword}");

        Assert.Equal(match, Assert.Single(items).GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task Search_KeywordInDescription_MatchesTheProduct()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        string keyword = $"Desc{Guid.NewGuid():N}";
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);
        long match = await CreateProductAsync(admin, categoryId, name: "Plain name", description: $"Mentions {keyword} inside");

        JsonElement[] items = await SearchAsync(admin, $"keyword={keyword}");

        Assert.Equal(match, Assert.Single(items).GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task Search_KeywordInTheNameOfOneAndTheDescriptionOfAnother_MatchesBoth()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        string keyword = $"Both{Guid.NewGuid():N}";
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);
        long byName = await CreateProductAsync(admin, categoryId, name: $"{keyword} thing", description: "Plain");
        long byDescription = await CreateProductAsync(admin, categoryId, name: "Plain", description: $"About {keyword}");

        JsonElement[] items = await SearchAsync(admin, $"keyword={keyword}");

        long[] ids = [.. items.Select(item => item.GetProperty("id").GetInt64())];
        Assert.Equal(2, ids.Length);
        Assert.Contains(byName, ids);
        Assert.Contains(byDescription, ids);
    }

    [Fact]
    public async Task Search_KeywordWithSurroundingWhitespace_IsTrimmed()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        string keyword = $"Trim{Guid.NewGuid():N}";
        long match = await CatalogApi.CreateProductAsync(admin, price: 1, quantity: 1, name: $"{keyword} widget");

        JsonElement[] items = await SearchAsync(admin, $"keyword=%20%20{keyword}%20%20");

        Assert.Equal(match, Assert.Single(items).GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task Search_KeywordMatchingNothing_ReturnsNoProducts()
    {
        using HttpClient anonymous = SystemFixture.CreateClient(system.Catalog);

        JsonElement page = await anonymous.GetFromJsonAsync<JsonElement>($"/api/v1/products?keyword=Nothing{Guid.NewGuid():N}", Ct);

        Assert.Equal(0, page.GetProperty("items").GetArrayLength());
        Assert.Equal(0, page.GetProperty("totalCount").GetInt64());
    }

    [Theory]
    [InlineData("")]
    [InlineData("%20%20%20")]
    public async Task Search_BlankKeyword_MatchesEverything(string keyword)
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);
        await CatalogApi.CreateProductAsync(admin, price: 1, quantity: 1, categoryId: categoryId);
        await CatalogApi.CreateProductAsync(admin, price: 2, quantity: 1, categoryId: categoryId);

        JsonElement[] items = await SearchAsync(admin, $"categoryId={categoryId}&keyword={keyword}");

        Assert.Equal(2, items.Length);
    }

    [Fact]
    public async Task Search_ByCategoryAndPriceRange_FiltersProducts()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);
        await CatalogApi.CreateProductAsync(admin, price: 5, quantity: 1, categoryId: categoryId);
        long inRange = await CatalogApi.CreateProductAsync(admin, price: 15, quantity: 1, categoryId: categoryId);
        await CatalogApi.CreateProductAsync(admin, price: 25, quantity: 1, categoryId: categoryId);
        await CatalogApi.CreateProductAsync(admin, price: 15, quantity: 1);

        JsonElement[] items = await SearchAsync(admin, $"categoryId={categoryId}&minPrice=10&maxPrice=20");

        Assert.Equal(inRange, Assert.Single(items).GetProperty("id").GetInt64());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    public async Task Search_Page_IsAtLeastOne(int requestedPage, int expectedPage)
    {
        using HttpClient anonymous = SystemFixture.CreateClient(system.Catalog);

        JsonElement page = await anonymous.GetFromJsonAsync<JsonElement>($"/api/v1/products?page={requestedPage}", Ct);

        Assert.Equal(expectedPage, page.GetProperty("page").GetInt32());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 1)]
    [InlineData(25, 25)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(10_000, 100)]
    public async Task Search_PageSize_IsClampedBetweenOneAndOneHundred(int requestedPageSize, int expectedPageSize)
    {
        using HttpClient anonymous = SystemFixture.CreateClient(system.Catalog);

        JsonElement page = await anonymous.GetFromJsonAsync<JsonElement>($"/api/v1/products?pageSize={requestedPageSize}", Ct);

        Assert.Equal(expectedPageSize, page.GetProperty("pageSize").GetInt32());
        Assert.True(page.GetProperty("items").GetArrayLength() <= expectedPageSize);
    }

    [Fact]
    public async Task Search_Paging_ReturnsTheRequestedSliceAndTheTotalCount()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);

        foreach (string name in new[] { "A", "B", "C" })
        {
            await CatalogApi.CreateProductAsync(admin, price: 1, quantity: 1, name: name, categoryId: categoryId);
        }

        JsonElement page = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/v1/products?categoryId={categoryId}&sortColumn=name&sortOrder=asc&page=2&pageSize=2",
            Ct);

        Assert.Equal(3, page.GetProperty("totalCount").GetInt64());
        JsonElement item = Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal("C", item.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Search_SortByNameDescending_OrdersByNameDescending()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);

        foreach (string name in new[] { "B", "C", "A" })
        {
            await CatalogApi.CreateProductAsync(admin, price: 1, quantity: 1, name: name, categoryId: categoryId);
        }

        JsonElement[] items = await SearchAsync(admin, $"categoryId={categoryId}&sortColumn=name&sortOrder=desc");

        Assert.Equal(["C", "B", "A"], items.Select(item => item.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task Search_SortByPrice_OrdersByPrice()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);

        foreach (decimal price in new[] { 30m, 10m, 20m })
        {
            await CatalogApi.CreateProductAsync(admin, price: price, quantity: 1, categoryId: categoryId);
        }

        JsonElement[] items = await SearchAsync(admin, $"categoryId={categoryId}&sortColumn=priceAmount");

        Assert.Equal([10m, 20m, 30m], items.Select(item => item.GetProperty("priceAmount").GetDecimal()));
    }

    [Fact]
    public async Task Search_MapsEveryFieldOfTheProduct()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);
        long productId = await CreateProductAsync(admin, categoryId, name: "Mapped", description: "Mapping check", price: 42.5m, quantity: 3);

        JsonElement item = Assert.Single(await SearchAsync(admin, $"categoryId={categoryId}"));

        Assert.Equal(productId, item.GetProperty("id").GetInt64());
        Assert.Equal("Mapped", item.GetProperty("name").GetString());
        Assert.Equal("Mapping check", item.GetProperty("description").GetString());
        Assert.Equal(42.5m, item.GetProperty("priceAmount").GetDecimal());
        Assert.Equal("BDT", item.GetProperty("priceCurrency").GetString());
        Assert.Equal(3, item.GetProperty("quantity").GetInt32());
        Assert.Equal(categoryId, item.GetProperty("categoryId").GetInt64());
        Assert.NotEqual(JsonValueKind.Null, item.GetProperty("createdOnUtc").ValueKind);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<long> CreateProductAsync(HttpClient admin, long categoryId, string name, string description, decimal price = 1m, int quantity = 1)
    {
        HttpResponseMessage response = await admin.PostAsJsonAsync(
            "/api/v1/products",
            new { name, description, priceCurrency = "BDT", priceAmount = price, quantity, categoryId },
            Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await CatalogApi.ReadIdAsync(response);
    }

    private static Task<HttpResponseMessage> EditAsync(HttpClient client, long id, long categoryId, string name = "Edited", decimal price = 10m, int quantity = 1)
    {
        return client.PutAsJsonAsync(
            $"/api/v1/products/{id}",
            new { name, description = "Edited by an integration test", priceCurrency = "BDT", priceAmount = price, quantity, categoryId },
            Ct);
    }

    private static async Task<JsonElement> GetProductAsync(HttpClient client, long id)
    {
        HttpResponseMessage response = await client.GetAsync($"/api/v1/products/{id}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await CatalogTestSupport.ReadJsonAsync(response);
    }

    private static async Task<JsonElement[]> SearchAsync(HttpClient client, string query)
    {
        JsonElement page = await client.GetFromJsonAsync<JsonElement>($"/api/v1/products?{query}", Ct);

        return [.. page.GetProperty("items").EnumerateArray()];
    }
}
