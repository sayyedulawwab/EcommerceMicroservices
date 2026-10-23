using EcommerceMicroservices.IntegrationTests.Infrastructure;
using Identity.Domain.Users;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests.Identity;

[Collection(SystemCollection.Name)]
public sealed class ProfileTests(SystemFixture system)
{
    private const string Route = "/api/v1/users/me";

    private readonly IdentityApi _api = new(system);

    [Fact]
    public async Task GetProfile_ReturnsTheSignedInUsersAccount()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.LoginAsync(browser.Client, email);

        JsonElement me = await browser.Client.GetFromJsonAsync<JsonElement>(Route, IdentityApi.Ct);

        Assert.Equal(email, me.GetProperty("email").GetString());
        Assert.Equal("Test", me.GetProperty("firstName").GetString());
        Assert.Equal("User", me.GetProperty("lastName").GetString());
        Assert.False(me.GetProperty("emailConfirmed").GetBoolean());
    }

    [Fact]
    public async Task GetProfile_UnknownUser_Returns404()
    {
        using HttpClient client = system.CreateCookielessIdentityClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create(TestJwt.NewUserId()));

        HttpResponseMessage response = await client.GetAsync(Route, IdentityApi.Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ValidNames_AreStored()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.LoginAsync(browser.Client, email);

        HttpResponseMessage response = await browser.Client.PutAsJsonAsync(Route, new { firstName = "Jane", lastName = "Doe" }, IdentityApi.Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        User user = await _api.GetUserAsync(email);
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.NotNull(user.UpdatedOnUtc);
    }

    [Fact]
    public async Task UpdateProfile_EmptyNames_Returns400AndChangesNothing()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.LoginAsync(browser.Client, email);

        HttpResponseMessage response = await browser.Client.PutAsJsonAsync(Route, new { firstName = "", lastName = "" }, IdentityApi.Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        User user = await _api.GetUserAsync(email);
        Assert.Equal("Test", user.FirstName);
        Assert.Equal("User", user.LastName);
    }

    [Fact]
    public async Task UpdateProfile_UnknownUser_Returns404()
    {
        using HttpClient client = system.CreateCookielessIdentityClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create(TestJwt.NewUserId()));

        HttpResponseMessage response = await client.PutAsJsonAsync(Route, new { firstName = "Jane", lastName = "Doe" }, IdentityApi.Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
