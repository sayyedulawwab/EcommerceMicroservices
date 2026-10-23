using EcommerceMicroservices.IntegrationTests.Infrastructure;
using Identity.Domain.Users;
using System.Net;

namespace EcommerceMicroservices.IntegrationTests.Identity;

[Collection(SystemCollection.Name)]
public sealed class LogoutTests(SystemFixture system)
{
    private readonly IdentityApi _api = new(system);

    [Fact]
    public async Task Logout_KnownToken_RevokesItsWholeFamily_AndTheTokenStopsWorking()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.LoginAsync(browser.Client, email);
        await IdentityApi.PostWithoutBodyAsync(browser.Client, "auth/refresh");
        string raw = browser.GetCookie("refresh_token")!;

        HttpResponseMessage response = await _api.PostWithRefreshCookieAsync("auth/logout", raw);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        User user = await _api.GetUserAsync(email);
        Assert.All(await _api.RefreshTokensAsync(user.Id), token => Assert.NotNull(token.RevokedOnUtc));
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.RefreshAsync(raw)).StatusCode);
    }

    [Fact]
    public async Task Logout_LeavesOtherDevicesSignedIn()
    {
        using BrowserSession laptop = _api.NewBrowser();
        using BrowserSession phone = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(laptop.Client);
        await IdentityApi.LoginAsync(laptop.Client, email);
        await IdentityApi.LoginAsync(phone.Client, email);

        await IdentityApi.PostWithoutBodyAsync(laptop.Client, "auth/logout");

        Assert.Equal(HttpStatusCode.NoContent, (await IdentityApi.PostWithoutBodyAsync(phone.Client, "auth/refresh")).StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutCookie_Returns204AndRevokesNothing()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.LoginAsync(browser.Client, email);

        HttpResponseMessage response = await _api.PostWithRefreshCookieAsync("auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        User user = await _api.GetUserAsync(email);
        Assert.All(await _api.RefreshTokensAsync(user.Id), token => Assert.Null(token.RevokedOnUtc));
    }

    [Fact]
    public async Task Logout_UnknownToken_Returns204AndRevokesNothing()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.LoginAsync(browser.Client, email);

        HttpResponseMessage response = await _api.PostWithRefreshCookieAsync("auth/logout", "not-a-token-we-issued");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        User user = await _api.GetUserAsync(email);
        Assert.All(await _api.RefreshTokensAsync(user.Id), token => Assert.Null(token.RevokedOnUtc));
    }
}
