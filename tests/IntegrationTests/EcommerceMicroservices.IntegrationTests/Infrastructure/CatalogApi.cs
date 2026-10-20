using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests.Infrastructure;

/// <summary>Catalog calls the tests use to set up data.</summary>
public static class CatalogApi
{
    public static async Task<long> CreateCategoryAsync(HttpClient adminClient, long? parentCategoryId = null)
    {
        HttpResponseMessage response = await adminClient.PostAsJsonAsync(
            "/api/v1/categories",
            new { name = $"Category {Guid.NewGuid():N}", description = "Created by an integration test", parentCategoryId },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await ReadIdAsync(response);
    }

    public static async Task<long> CreateProductAsync(HttpClient adminClient, decimal price, int quantity, string? name = null, long? categoryId = null)
    {
        long category = categoryId ?? await CreateCategoryAsync(adminClient);

        HttpResponseMessage response = await adminClient.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                name = name ?? $"Product {Guid.NewGuid():N}",
                description = "Created by an integration test",
                priceCurrency = "BDT",
                priceAmount = price,
                quantity,
                categoryId = category
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await ReadIdAsync(response);
    }

    public static async Task<int> GetStockAsync(HttpClient client, long productId)
    {
        JsonElement product = await client.GetFromJsonAsync<JsonElement>($"/api/v1/products/{productId}", TestContext.Current.CancellationToken);

        return product.GetProperty("quantity").GetInt32();
    }

    public static async Task<long> ReadIdAsync(HttpResponseMessage response)
    {
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        return body.GetProperty("id").GetInt64();
    }
}
