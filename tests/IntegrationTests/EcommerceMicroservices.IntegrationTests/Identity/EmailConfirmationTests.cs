using EcommerceMicroservices.IntegrationTests.Infrastructure;
using Identity.Domain.Users;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests.Identity;

[Collection(SystemCollection.Name)]
public sealed class EmailConfirmationTests(SystemFixture system)
{
    private readonly IdentityApi _api = new(system);

    [Fact]
    public async Task ConfirmEmail_ValidToken_ConfirmsTheEmailAndConsumesTheToken()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);

        HttpResponseMessage response = await IdentityApi.ConfirmEmailAsync(browser.Client, email, IdentityApi.ConfirmationTokenFor(email));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        User user = await _api.GetUserAsync(email);
        Assert.True(user.EmailConfirmed);
        Assert.NotNull(Assert.Single(await _api.UserTokensAsync(user.Id, UserTokenPurpose.EmailConfirmation)).ConsumedOnUtc);

        await IdentityApi.LoginAsync(browser.Client, email);
        JsonElement me = await browser.Client.GetFromJsonAsync<JsonElement>("/api/v1/users/me", IdentityApi.Ct);
        Assert.True(me.GetProperty("emailConfirmed").GetBoolean());
    }

    [Fact]
    public async Task ConfirmEmail_UnknownEmail_Returns400WithInvalidOrExpired()
    {
        using BrowserSession browser = _api.NewBrowser();

        HttpResponseMessage response = await IdentityApi.ConfirmEmailAsync(browser.Client, IdentityApi.NewEmail(), "whatever");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("UserToken.InvalidOrExpired", await response.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConfirmEmail_UnknownToken_Returns400AndLeavesTheEmailUnconfirmed()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);

        HttpResponseMessage response = await IdentityApi.ConfirmEmailAsync(browser.Client, email, "not-the-mailed-token");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("UserToken.InvalidOrExpired", await response.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);
        Assert.False((await _api.GetUserAsync(email)).EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmail_PasswordResetTokenOfTheSameUser_IsRejected()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.PostJsonAsync(browser.Client, "auth/forgot-password", new { email });

        HttpResponseMessage response = await IdentityApi.ConfirmEmailAsync(browser.Client, email, IdentityApi.ResetTokenFor(email));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False((await _api.GetUserAsync(email)).EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmail_ExpiredToken_Returns400AndLeavesTheEmailUnconfirmed()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        string token = IdentityApi.ConfirmationTokenFor(email);
        await _api.ExpireUserTokenAsync(token);

        HttpResponseMessage response = await IdentityApi.ConfirmEmailAsync(browser.Client, email, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False((await _api.GetUserAsync(email)).EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmail_AlreadyConsumedToken_Returns400()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        string oldToken = IdentityApi.ConfirmationTokenFor(email);
        await IdentityApi.PostJsonAsync(browser.Client, "auth/resend-confirmation", new { email });

        // The resend consumed the first link.
        HttpResponseMessage response = await IdentityApi.ConfirmEmailAsync(browser.Client, email, oldToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False((await _api.GetUserAsync(email)).EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmail_AlreadyConfirmedWithTheMailedToken_StillSucceeds()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        string token = IdentityApi.ConfirmationTokenFor(email);
        await IdentityApi.ConfirmEmailAsync(browser.Client, email, token);

        HttpResponseMessage secondClick = await IdentityApi.ConfirmEmailAsync(browser.Client, email, token);

        Assert.Equal(HttpStatusCode.NoContent, secondClick.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_AlreadyConfirmedWithAWrongToken_Returns400_SoAccountsCantBeProbed()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.ConfirmEmailAsync(browser.Client, email, IdentityApi.ConfirmationTokenFor(email));

        HttpResponseMessage response = await IdentityApi.ConfirmEmailAsync(browser.Client, email, "guess");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResendConfirmation_UnknownEmail_Returns202AndSendsNothing()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = IdentityApi.NewEmail();

        HttpResponseMessage response = await IdentityApi.PostJsonAsync(browser.Client, "auth/resend-confirmation", new { email });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(CapturingEmailSender.To(email));
    }

    [Fact]
    public async Task ResendConfirmation_AlreadyConfirmed_Returns202AndIssuesNothing()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.ConfirmEmailAsync(browser.Client, email, IdentityApi.ConfirmationTokenFor(email));

        HttpResponseMessage response = await IdentityApi.PostJsonAsync(browser.Client, "auth/resend-confirmation", new { email });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Single(CapturingEmailSender.To(email));
        User user = await _api.GetUserAsync(email);
        Assert.Single(await _api.UserTokensAsync(user.Id, UserTokenPurpose.EmailConfirmation));
    }

    [Fact]
    public async Task ResendConfirmation_UnconfirmedUser_ConsumesOldLinksAndMailsANewWorkingOne()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        string oldToken = IdentityApi.ConfirmationTokenFor(email);

        HttpResponseMessage response = await IdentityApi.PostJsonAsync(browser.Client, "auth/resend-confirmation", new { email });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(2, CapturingEmailSender.To(email).Count);

        User user = await _api.GetUserAsync(email);
        List<UserToken> tokens = await _api.UserTokensAsync(user.Id, UserTokenPurpose.EmailConfirmation);
        Assert.Equal(2, tokens.Count);
        Assert.NotNull(tokens[0].ConsumedOnUtc);
        Assert.Null(tokens[1].ConsumedOnUtc);

        string newToken = IdentityApi.ConfirmationTokenFor(email);
        Assert.NotEqual(oldToken, newToken);
        Assert.Equal(HttpStatusCode.BadRequest, (await IdentityApi.ConfirmEmailAsync(browser.Client, email, oldToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityApi.ConfirmEmailAsync(browser.Client, email, newToken)).StatusCode);
    }
}
