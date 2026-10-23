using EcommerceMicroservices.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests.Catalog;

[Collection(SystemCollection.Name)]
public sealed class CategoryApiTests(SystemFixture system)
{
    [Fact]
    public async Task AddCategory_WithoutParent_CreatesARootCategory()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);

        HttpResponseMessage response = await admin.PostAsJsonAsync("/api/v1/categories", new { name = "Root", description = "A root category" }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        JsonElement category = await GetCategoryAsync(admin, await CatalogApi.ReadIdAsync(response));
        Assert.Equal("Root", category.GetProperty("name").GetString());
        Assert.Equal("A root category", category.GetProperty("description").GetString());
        Assert.Equal(JsonValueKind.Null, category.GetProperty("parentCategoryId").ValueKind);
    }

    [Fact]
    public async Task AddCategory_WithExistingParent_CreatesAChildCategory()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long parentId = await CatalogApi.CreateCategoryAsync(admin);

        long childId = await CatalogApi.CreateCategoryAsync(admin, parentCategoryId: parentId);

        JsonElement child = await GetCategoryAsync(admin, childId);
        Assert.Equal(parentId, child.GetProperty("parentCategoryId").GetInt64());
    }

    [Fact]
    public async Task AddCategory_WithUnknownParent_IsRejected_AndAddsNothing()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        string name = $"Orphan {Guid.NewGuid():N}";

        HttpResponseMessage response = await admin.PostAsJsonAsync(
            "/api/v1/categories",
            new { name, description = "x", parentCategoryId = long.MaxValue },
            Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Category.ParentNotFound", await CatalogTestSupport.ReadErrorNameAsync(response));
        Assert.DoesNotContain(await GetAllCategoriesAsync(admin), c => c.GetProperty("name").GetString() == name);
    }

    [Fact]
    public async Task AddCategory_WithInvalidData_IsRejected()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);

        HttpResponseMessage response = await admin.PostAsJsonAsync("/api/v1/categories", new { name = "", description = "x" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CategoryWrites_RequireTheAdminRole()
    {
        using HttpClient anonymous = SystemFixture.CreateClient(system.Catalog);
        using HttpClient customer = SystemFixture.CreateClient(system.Catalog, TestJwt.Create(TestJwt.NewUserId()));
        var category = new { name = "Not allowed", description = "x" };

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/categories", category, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PostAsJsonAsync("/api/v1/categories", category, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PutAsJsonAsync("/api/v1/categories/1", category, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.DeleteAsync("/api/v1/categories/1", Ct)).StatusCode);
    }

    [Fact]
    public async Task GetCategory_Unknown_IsNotFound()
    {
        using HttpClient anonymous = SystemFixture.CreateClient(system.Catalog);

        HttpResponseMessage response = await anonymous.GetAsync($"/api/v1/categories/{long.MaxValue}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_ListsEveryCategory()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long first = await CatalogApi.CreateCategoryAsync(admin);
        long second = await CatalogApi.CreateCategoryAsync(admin, parentCategoryId: first);

        JsonElement[] categories = await GetAllCategoriesAsync(admin);

        Assert.Contains(categories, c => c.GetProperty("id").GetInt64() == first);
        JsonElement child = Assert.Single(categories, c => c.GetProperty("id").GetInt64() == second);
        Assert.Equal(first, child.GetProperty("parentCategoryId").GetInt64());
    }

    [Fact]
    public async Task DeleteCategory_Unused_RemovesIt()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);

        HttpResponseMessage response = await admin.DeleteAsync($"/api/v1/categories/{categoryId}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/categories/{categoryId}", Ct)).StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithChildren_IsAConflict_AndKeepsIt()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);
        await CatalogApi.CreateCategoryAsync(admin, parentCategoryId: categoryId);

        HttpResponseMessage response = await admin.DeleteAsync($"/api/v1/categories/{categoryId}", Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Category.InUse", await CatalogTestSupport.ReadErrorNameAsync(response));
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/categories/{categoryId}", Ct)).StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithProducts_IsAConflict_AndKeepsIt()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long categoryId = await CatalogApi.CreateCategoryAsync(admin);
        await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 1, categoryId: categoryId);

        HttpResponseMessage response = await admin.DeleteAsync($"/api/v1/categories/{categoryId}", Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Category.InUse", await CatalogTestSupport.ReadErrorNameAsync(response));
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/categories/{categoryId}", Ct)).StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_Unknown_IsNotFound()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);

        HttpResponseMessage response = await admin.DeleteAsync($"/api/v1/categories/{long.MaxValue}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EditCategory_Unknown_IsNotFound()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);

        HttpResponseMessage response = await EditAsync(admin, long.MaxValue, parentCategoryId: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EditCategory_WithParentInAnotherBranch_UpdatesTheCategory()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long otherBranch = await CatalogApi.CreateCategoryAsync(admin);
        long category = await CatalogApi.CreateCategoryAsync(admin);

        HttpResponseMessage response = await EditAsync(admin, category, otherBranch, name: "Renamed", description: "New description");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement stored = await GetCategoryAsync(admin, category);
        Assert.Equal("Renamed", stored.GetProperty("name").GetString());
        Assert.Equal("New description", stored.GetProperty("description").GetString());
        Assert.Equal(otherBranch, stored.GetProperty("parentCategoryId").GetInt64());
        Assert.NotEqual(JsonValueKind.Null, stored.GetProperty("updatedOnUtc").ValueKind);
    }

    [Fact]
    public async Task EditCategory_WithNullParent_MakesItARootCategory()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long parent = await CatalogApi.CreateCategoryAsync(admin);
        long child = await CatalogApi.CreateCategoryAsync(admin, parentCategoryId: parent);

        HttpResponseMessage response = await EditAsync(admin, child, parentCategoryId: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await GetCategoryAsync(admin, child)).GetProperty("parentCategoryId").ValueKind);
    }

    [Fact]
    public async Task EditCategory_WithUnknownParent_IsRejected()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long category = await CatalogApi.CreateCategoryAsync(admin);

        HttpResponseMessage response = await EditAsync(admin, category, long.MaxValue);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Category.ParentNotFound", await CatalogTestSupport.ReadErrorNameAsync(response));
    }

    [Fact]
    public async Task EditCategory_WithItselfAsParent_IsRejected()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long category = await CatalogApi.CreateCategoryAsync(admin);

        HttpResponseMessage response = await EditAsync(admin, category, category);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Category.CircularParent", await CatalogTestSupport.ReadErrorNameAsync(response));
    }

    [Fact]
    public async Task EditCategory_WithItsDirectChildAsParent_IsRejected()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long category = await CatalogApi.CreateCategoryAsync(admin);
        long child = await CatalogApi.CreateCategoryAsync(admin, parentCategoryId: category);

        HttpResponseMessage response = await EditAsync(admin, category, child);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Category.CircularParent", await CatalogTestSupport.ReadErrorNameAsync(response));
        Assert.Equal(JsonValueKind.Null, (await GetCategoryAsync(admin, category)).GetProperty("parentCategoryId").ValueKind);
    }

    [Fact]
    public async Task EditCategory_WithADescendantAsParent_IsRejected()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        long category = await CatalogApi.CreateCategoryAsync(admin);
        long child = await CatalogApi.CreateCategoryAsync(admin, parentCategoryId: category);
        long grandChild = await CatalogApi.CreateCategoryAsync(admin, parentCategoryId: child);

        HttpResponseMessage response = await EditAsync(admin, category, grandChild);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Category.CircularParent", await CatalogTestSupport.ReadErrorNameAsync(response));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> EditAsync(HttpClient admin, long id, long? parentCategoryId, string name = "Edited", string description = "Edited by an integration test")
    {
        return admin.PutAsJsonAsync($"/api/v1/categories/{id}", new { name, description, parentCategoryId }, Ct);
    }

    private static async Task<JsonElement> GetCategoryAsync(HttpClient client, long id)
    {
        HttpResponseMessage response = await client.GetAsync($"/api/v1/categories/{id}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await CatalogTestSupport.ReadJsonAsync(response);
    }

    private static async Task<JsonElement[]> GetAllCategoriesAsync(HttpClient client)
    {
        JsonElement categories = await client.GetFromJsonAsync<JsonElement>("/api/v1/categories", Ct);

        return [.. categories.EnumerateArray()];
    }
}
