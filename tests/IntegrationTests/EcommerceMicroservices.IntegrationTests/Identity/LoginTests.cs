using EcommerceMicroservices.IntegrationTests.Infrastructure;
using Identity.Domain.Users;
using System.Net;

namespace EcommerceMicroservices.IntegrationTests.Identity;

[Collection(SystemCollection.Name)]
public sealed class LoginTests(SystemFixture system)
{
    private readonly IdentityApi _api = new(system);

    [Fact]
    public async Task Login_UnknownEmail_Returns401WithInvalidCredentials()
    {
        using BrowserSession browser = _api.NewBrowser();

        HttpResponseMessage response = await IdentityApi.LoginRawAsync(browser.Client, IdentityApi.NewEmail());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("User.InvalidCredentials", await response.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);
        Assert.Null(browser.GetCookie("access_token"));
    }

    [Fact]
    public async Task Login_WrongPassword_GivesTheSameAnswerAsUnknownEmail_AndStoresNoSession()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);

        HttpResponseMessage response = await IdentityApi.LoginRawAsync(browser.Client, email, "Wrong" + IdentityApi.Password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("User.InvalidCredentials", await response.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);
        Assert.Null(browser.GetCookie("refresh_token"));

        User user = await _api.GetUserAsync(email);
        Assert.Empty(await _api.RefreshTokensAsync(user.Id));
    }

    [Fact]
    public async Task Login_EmailInDifferentCase_Succeeds()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);

        HttpResponseMessage response = await IdentityApi.LoginRawAsync(browser.Client, email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Login_ConfirmationRequiredAndEmailUnconfirmed_Returns403AndStoresNoSession()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);

        using (_api.OverrideAccountOptions(options => options.RequireConfirmedEmail = true))
        {
            HttpResponseMessage response = await IdentityApi.LoginRawAsync(browser.Client, email);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Contains("User.EmailNotConfirmed", await response.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);
        }

        Assert.Null(browser.GetCookie("access_token"));

        User user = await _api.GetUserAsync(email);
        Assert.Empty(await _api.RefreshTokensAsync(user.Id));
    }

    [Fact]
    public async Task Login_ConfirmationRequiredAndEmailConfirmed_Succeeds()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        HttpResponseMessage confirm = await IdentityApi.ConfirmEmailAsync(browser.Client, email, IdentityApi.ConfirmationTokenFor(email));
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        using (_api.OverrideAccountOptions(options => options.RequireConfirmedEmail = true))
        {
            HttpResponseMessage response = await IdentityApi.LoginRawAsync(browser.Client, email);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    [Fact]
    public async Task Login_ConfirmationNotRequiredAndEmailUnconfirmed_Succeeds()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);

        HttpResponseMessage response = await IdentityApi.LoginRawAsync(browser.Client, email);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_StoresOneHashedRefreshTokenInANewFamily()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);

        await IdentityApi.LoginAsync(browser.Client, email);

        string rawRefreshToken = browser.GetCookie("refresh_token")!;
        User user = await _api.GetUserAsync(email);
        RefreshToken stored = Assert.Single(await _api.RefreshTokensAsync(user.Id));

        Assert.Equal(_api.Hash(rawRefreshToken), stored.TokenHash);
        Assert.NotEqual(rawRefreshToken, stored.TokenHash);
        Assert.Null(stored.RevokedOnUtc);
        Assert.NotEqual(Guid.Empty, stored.FamilyId);
        Assert.True(stored.ExpiresOnUtc > DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task Login_Twice_StartsTwoDifferentFamilies()
    {
        using BrowserSession first = _api.NewBrowser();
        using BrowserSession second = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(first.Client);

        await IdentityApi.LoginAsync(first.Client, email);
        await IdentityApi.LoginAsync(second.Client, email);

        User user = await _api.GetUserAsync(email);
        List<RefreshToken> tokens = await _api.RefreshTokensAsync(user.Id);

        Assert.Equal(2, tokens.Count);
        Assert.NotEqual(tokens[0].FamilyId, tokens[1].FamilyId);
    }

    [Fact]
    public async Task Login_MissingPassword_Returns400()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);

        HttpResponseMessage response = await IdentityApi.LoginRawAsync(browser.Client, email, "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
