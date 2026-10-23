using EcommerceMicroservices.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests;

[Collection(SystemCollection.Name)]
public sealed class CatalogApiTests(SystemFixture system)
{
    [Fact]
    public async Task Products_CanBeBrowsedAnonymously()
    {
        using HttpClient anonymous = SystemFixture.CreateClient(system.Catalog);

        HttpResponseMessage search = await anonymous.GetAsync("/api/v1/products", Ct);
        HttpResponseMessage categories = await anonymous.GetAsync("/api/v1/categories", Ct);

        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        Assert.Equal(HttpStatusCode.OK, categories.StatusCode);
    }

    [Fact]
    public async Task Search_ByKeyword_MatchesNameAndDescription()
    {
        using HttpClient admin = SystemFixture.CreateClient(system.Catalog, TestJwt.Create(TestJwt.NewUserId(), isAdmin: true));
        string keyword = $"Zeta{Guid.NewGuid():N}";
        Guid productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 1, name: $"{keyword} Widget");

        using HttpClient anonymous = SystemFixture.CreateClient(system.Catalog);
        JsonElement page = await anonymous.GetFromJsonAsync<JsonElement>($"/api/v1/products?keyword={keyword}", Ct);

        JsonElement[] items = [.. page.GetProperty("items").EnumerateArray()];
        Assert.Single(items);
        Assert.Equal(productId, items[0].GetProperty("id").GetGuid());
        Assert.Equal(1, page.GetProperty("totalCount").GetInt64());
    }

    [Fact]
    public async Task ProductWrites_RequireTheAdminRole()
    {
        using HttpClient anonymous = SystemFixture.CreateClient(system.Catalog);
        using HttpClient customer = SystemFixture.CreateClient(system.Catalog, TestJwt.Create(TestJwt.NewUserId()));
        var product = new { name = "Not allowed", description = "x", priceCurrency = "BDT", priceAmount = 1m, quantity = 1, categoryId = Guid.CreateVersion7() };

        HttpResponseMessage anonymousResponse = await anonymous.PostAsJsonAsync("/api/v1/products", product, Ct);
        HttpResponseMessage customerResponse = await customer.PostAsJsonAsync("/api/v1/products", product, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, customerResponse.StatusCode);
    }

    [Fact]
    public async Task AddProduct_WithUnknownCategory_IsRejected()
    {
        using HttpClient admin = SystemFixture.CreateClient(system.Catalog, TestJwt.Create(TestJwt.NewUserId(), isAdmin: true));

        HttpResponseMessage response = await admin.PostAsJsonAsync(
            "/api/v1/products",
            new { name = "Orphan", description = "x", priceCurrency = "BDT", priceAmount = 1m, quantity = 1, categoryId = Guid.CreateVersion7() },
            Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_StillInUse_IsAConflict()
    {
        using HttpClient admin = SystemFixture.CreateClient(system.Catalog, TestJwt.Create(TestJwt.NewUserId(), isAdmin: true));
        Guid categoryId = await CatalogApi.CreateCategoryAsync(admin);
        await CatalogApi.CreateCategoryAsync(admin, parentCategoryId: categoryId);

        HttpResponseMessage response = await admin.DeleteAsync($"/api/v1/categories/{categoryId}", Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
