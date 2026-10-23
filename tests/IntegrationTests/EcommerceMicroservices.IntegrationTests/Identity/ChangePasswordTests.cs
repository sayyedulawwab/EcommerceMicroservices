using EcommerceMicroservices.IntegrationTests.Infrastructure;
using Identity.Domain.Users;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace EcommerceMicroservices.IntegrationTests.Identity;

[Collection(SystemCollection.Name)]
public sealed class ChangePasswordTests(SystemFixture system)
{
    private const string NewPassword = "An0therSecretPw";
    private const string Route = "/api/v1/users/me/password";

    private readonly IdentityApi _api = new(system);

    [Fact]
    public async Task ChangePassword_UnknownUser_Returns404()
    {
        using HttpClient client = system.CreateCookielessIdentityClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create(TestJwt.NewUserId()));

        HttpResponseMessage response = await client.PutAsJsonAsync(
            Route,
            new { currentPassword = IdentityApi.Password, newPassword = NewPassword },
            IdentityApi.Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("User.NotFound", await response.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400AndChangesNothing()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.LoginAsync(browser.Client, email);
        string refreshBefore = browser.GetCookie("refresh_token")!;

        HttpResponseMessage response = await browser.Client.PutAsJsonAsync(
            Route,
            new { currentPassword = "Wrong" + IdentityApi.Password, newPassword = NewPassword },
            IdentityApi.Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("User.InvalidCurrentPassword", await response.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);

        User user = await _api.GetUserAsync(email);
        RefreshToken stored = Assert.Single(await _api.RefreshTokensAsync(user.Id));
        Assert.Null(stored.RevokedOnUtc);
        Assert.Equal(refreshBefore, browser.GetCookie("refresh_token"));
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityApi.LoginRawAsync(browser.Client, email)).StatusCode);
    }

    [Fact]
    public async Task ChangePassword_CorrectCurrentPassword_ChangesThePasswordAndEndsEveryOtherSession()
    {
        using BrowserSession laptop = _api.NewBrowser();
        using BrowserSession phone = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(laptop.Client);
        await IdentityApi.LoginAsync(laptop.Client, email);
        await IdentityApi.LoginAsync(phone.Client, email);
        string laptopRefreshBefore = laptop.GetCookie("refresh_token")!;

        HttpResponseMessage response = await laptop.Client.PutAsJsonAsync(
            Route,
            new { currentPassword = IdentityApi.Password, newPassword = NewPassword },
            IdentityApi.Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // The old sessions are gone, including the caller's old refresh token and the other device.
        Assert.NotNull((await _api.FindRefreshTokenAsync(laptopRefreshBefore))!.RevokedOnUtc);
        Assert.Equal(HttpStatusCode.Unauthorized, (await IdentityApi.PostWithoutBodyAsync(phone.Client, "auth/refresh")).StatusCode);

        // The caller stays signed in with a fresh session in a new family.
        string laptopRefreshAfter = laptop.GetCookie("refresh_token")!;
        Assert.NotEqual(laptopRefreshBefore, laptopRefreshAfter);
        RefreshToken fresh = (await _api.FindRefreshTokenAsync(laptopRefreshAfter))!;
        Assert.Null(fresh.RevokedOnUtc);
        Assert.NotEqual((await _api.FindRefreshTokenAsync(laptopRefreshBefore))!.FamilyId, fresh.FamilyId);
        Assert.Equal(HttpStatusCode.OK, await IdentityApi.MeStatusAsync(laptop.Client));

        using BrowserSession newLogin = _api.NewBrowser();
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityApi.LoginRawAsync(newLogin.Client, email, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await IdentityApi.LoginRawAsync(newLogin.Client, email, IdentityApi.Password)).StatusCode);
    }

    [Fact]
    public async Task ChangePassword_CalledTwice_EachCallStartsADifferentFamily()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.LoginAsync(browser.Client, email);

        await browser.Client.PutAsJsonAsync(Route, new { currentPassword = IdentityApi.Password, newPassword = NewPassword }, IdentityApi.Ct);
        string afterFirst = browser.GetCookie("refresh_token")!;
        await browser.Client.PutAsJsonAsync(Route, new { currentPassword = NewPassword, newPassword = IdentityApi.Password }, IdentityApi.Ct);
        string afterSecond = browser.GetCookie("refresh_token")!;

        Assert.NotEqual(afterFirst, afterSecond);
        Assert.NotEqual(
            (await _api.FindRefreshTokenAsync(afterFirst))!.FamilyId,
            (await _api.FindRefreshTokenAsync(afterSecond))!.FamilyId);
    }

    [Fact]
    public async Task ChangePassword_NewPasswordEqualsCurrent_Returns400()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.LoginAsync(browser.Client, email);

        HttpResponseMessage response = await browser.Client.PutAsJsonAsync(
            Route,
            new { currentPassword = IdentityApi.Password, newPassword = IdentityApi.Password },
            IdentityApi.Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("The new password must be different from the current one", await response.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChangePassword_WeakNewPassword_Returns400AndKeepsThePassword()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.LoginAsync(browser.Client, email);

        HttpResponseMessage response = await browser.Client.PutAsJsonAsync(
            Route,
            new { currentPassword = IdentityApi.Password, newPassword = "weak" },
            IdentityApi.Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityApi.LoginRawAsync(browser.Client, email)).StatusCode);
    }
}
