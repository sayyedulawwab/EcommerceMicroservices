using EcommerceMicroservices.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests;

[Collection(SystemCollection.Name)]
public sealed class IdentityAuthTests(SystemFixture system)
{
    private const string UserPassword = "Sup3rSecretPw";
    private const string RequestedWithHeader = "X-Requested-With";

    [Fact]
    public async Task Login_SetsHttpOnlySecureCookies_AndMeReturnsTheUser()
    {
        using BrowserSession browser = system.CreateBrowserSession();
        string email = await RegisterAsync(browser.Client);

        HttpResponseMessage login = await browser.Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = UserPassword }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        string[] setCookies = [.. login.Headers.GetValues("Set-Cookie")];
        Assert.Contains(setCookies, c => c.StartsWith("access_token=", StringComparison.Ordinal) && IsHardened(c));
        Assert.Contains(setCookies, c => c.StartsWith("refresh_token=", StringComparison.Ordinal) && IsHardened(c));
        Assert.Empty(await login.Content.ReadAsByteArrayAsync(Ct));

        JsonElement me = await browser.Client.GetFromJsonAsync<JsonElement>("/api/v1/users/me", Ct);

        Assert.Equal(email, me.GetProperty("email").GetString());
        Assert.Empty(me.GetProperty("roles").EnumerateArray());
    }

    [Fact]
    public async Task Refresh_RotatesTheRefreshToken_AndTheOldOneStopsWorking()
    {
        using BrowserSession browser = system.CreateBrowserSession();
        await LoginAsync(browser.Client, await RegisterAsync(browser.Client));

        string? oldRefreshToken = browser.GetCookie("refresh_token");
        Assert.False(string.IsNullOrEmpty(oldRefreshToken));

        HttpResponseMessage refresh = await PostWithoutBodyAsync(browser.Client, "/api/v1/auth/refresh");

        Assert.Equal(HttpStatusCode.NoContent, refresh.StatusCode);
        Assert.NotEqual(oldRefreshToken, browser.GetCookie("refresh_token"));

        // Replaying the rotated-out token is refused.
        using HttpClient replayClient = system.CreateCookielessIdentityClient();
        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        replayRequest.Headers.Add("Cookie", $"refresh_token={oldRefreshToken}");
        replayRequest.Headers.Add(RequestedWithHeader, "XMLHttpRequest");
        HttpResponseMessage replay = await replayClient.SendAsync(replayRequest, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task Logout_ClearsTheCookies()
    {
        using BrowserSession browser = system.CreateBrowserSession();
        await LoginAsync(browser.Client, await RegisterAsync(browser.Client));

        HttpResponseMessage logout = await PostWithoutBodyAsync(browser.Client, "/api/v1/auth/logout");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Null(browser.GetCookie("access_token"));

        HttpResponseMessage me = await browser.Client.GetAsync("/api/v1/users/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task CookieAuthenticatedWrite_WithoutJsonOrRequestedWithHeader_IsRejectedAsCsrf()
    {
        using BrowserSession browser = system.CreateBrowserSession();
        await LoginAsync(browser.Client, await RegisterAsync(browser.Client));

        // What a forged cross-site form post looks like: cookies attached, no JSON, no custom header.
        HttpResponseMessage response = await browser.Client.PostAsync("/api/v1/auth/refresh", content: null, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        using BrowserSession browser = system.CreateBrowserSession();
        string email = await RegisterAsync(browser.Client);

        HttpResponseMessage login = await browser.Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Wrong" + UserPassword }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task SeededAdmin_HasTheAdminRole()
    {
        using BrowserSession browser = system.CreateBrowserSession();

        HttpResponseMessage login = await browser.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "admin@ecommerce.local", password = "Admin123!" },
            Ct);

        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);

        JsonElement me = await browser.Client.GetFromJsonAsync<JsonElement>("/api/v1/users/me", Ct);

        Assert.Contains(me.GetProperty("roles").EnumerateArray(), role => role.GetString() == "Admin");
    }

    [Fact]
    public async Task ForgotPassword_ForUnknownEmail_StillReturns202()
    {
        using BrowserSession browser = system.CreateBrowserSession();

        HttpResponseMessage response = await browser.Client.PostAsJsonAsync(
            "/api/v1/auth/forgot-password",
            new { email = $"nobody-{Guid.NewGuid():N}@test.local" },
            Ct);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static bool IsHardened(string setCookie) =>
        setCookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) &&
        setCookie.Contains("secure", StringComparison.OrdinalIgnoreCase) &&
        setCookie.Contains("samesite=none", StringComparison.OrdinalIgnoreCase);

    private static async Task<HttpResponseMessage> PostWithoutBodyAsync(HttpClient client, string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add(RequestedWithHeader, "XMLHttpRequest");

        return await client.SendAsync(request, Ct);
    }

    private static async Task<string> RegisterAsync(HttpClient client)
    {
        string email = $"user-{Guid.NewGuid():N}@test.local";

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { firstName = "Test", lastName = "User", email, password = UserPassword },
            Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return email;
    }

    private static async Task LoginAsync(HttpClient client, string email)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = UserPassword },
            Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
