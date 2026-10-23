using EcommerceMicroservices.IntegrationTests.Infrastructure;
using Identity.Domain.Users;
using System.Net;

namespace EcommerceMicroservices.IntegrationTests.Identity;

[Collection(SystemCollection.Name)]
public sealed class PasswordResetTests(SystemFixture system)
{
    private const string NewPassword = "An0therSecretPw";

    private readonly IdentityApi _api = new(system);

    [Fact]
    public async Task ForgotPassword_UnknownEmail_Returns202AndIssuesNothing()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = IdentityApi.NewEmail();

        HttpResponseMessage response = await IdentityApi.PostJsonAsync(browser.Client, "auth/forgot-password", new { email });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(CapturingEmailSender.To(email));
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmail_Returns400()
    {
        using BrowserSession browser = _api.NewBrowser();

        HttpResponseMessage response = await IdentityApi.PostJsonAsync(browser.Client, "auth/forgot-password", new { email = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_ConsumesOutstandingTokensThenMailsANewResetLink()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.PostJsonAsync(browser.Client, "auth/forgot-password", new { email });
        string firstToken = IdentityApi.ResetTokenFor(email);

        HttpResponseMessage response = await IdentityApi.PostJsonAsync(browser.Client, "auth/forgot-password", new { email });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        User user = await _api.GetUserAsync(email);
        List<UserToken> tokens = await _api.UserTokensAsync(user.Id, UserTokenPurpose.PasswordReset);
        Assert.Equal(2, tokens.Count);
        Assert.NotNull(tokens[0].ConsumedOnUtc);
        Assert.Null(tokens[1].ConsumedOnUtc);

        // Only the most recently mailed link works.
        Assert.Equal(HttpStatusCode.BadRequest, (await IdentityApi.ResetPasswordAsync(browser.Client, email, firstToken, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityApi.ResetPasswordAsync(browser.Client, email, IdentityApi.ResetTokenFor(email), NewPassword)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_SetsThePasswordConfirmsTheEmailAndEndsEverySession()
    {
        using BrowserSession laptop = _api.NewBrowser();
        using BrowserSession phone = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(laptop.Client);
        await IdentityApi.LoginAsync(laptop.Client, email);
        await IdentityApi.LoginAsync(phone.Client, email);
        await IdentityApi.PostJsonAsync(laptop.Client, "auth/forgot-password", new { email });
        string token = IdentityApi.ResetTokenFor(email);

        HttpResponseMessage response = await IdentityApi.ResetPasswordAsync(laptop.Client, email, token, NewPassword);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("access_token=;", StringComparison.Ordinal));

        User user = await _api.GetUserAsync(email);
        Assert.True(user.EmailConfirmed);
        Assert.NotNull(Assert.Single(await _api.UserTokensAsync(user.Id, UserTokenPurpose.PasswordReset)).ConsumedOnUtc);
        Assert.All(await _api.RefreshTokensAsync(user.Id), refreshToken => Assert.NotNull(refreshToken.RevokedOnUtc));
        Assert.Equal(HttpStatusCode.Unauthorized, (await IdentityApi.PostWithoutBodyAsync(phone.Client, "auth/refresh")).StatusCode);

        using BrowserSession newLogin = _api.NewBrowser();
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityApi.LoginRawAsync(newLogin.Client, email, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await IdentityApi.LoginRawAsync(newLogin.Client, email, IdentityApi.Password)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_UnknownEmail_Returns400WithInvalidOrExpired()
    {
        using BrowserSession browser = _api.NewBrowser();

        HttpResponseMessage response = await IdentityApi.ResetPasswordAsync(browser.Client, IdentityApi.NewEmail(), "whatever", NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("UserToken.InvalidOrExpired", await response.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResetPassword_UnknownToken_Returns400AndKeepsThePassword()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.PostJsonAsync(browser.Client, "auth/forgot-password", new { email });

        HttpResponseMessage response = await IdentityApi.ResetPasswordAsync(browser.Client, email, "not-the-mailed-token", NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertPasswordUnchangedAsync(browser.Client, email);
    }

    [Fact]
    public async Task ResetPassword_EmailConfirmationTokenOfTheSameUser_IsRejected()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);

        HttpResponseMessage response = await IdentityApi.ResetPasswordAsync(browser.Client, email, IdentityApi.ConfirmationTokenFor(email), NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertPasswordUnchangedAsync(browser.Client, email);
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_Returns400AndKeepsThePassword()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.PostJsonAsync(browser.Client, "auth/forgot-password", new { email });
        string token = IdentityApi.ResetTokenFor(email);
        await _api.ExpireUserTokenAsync(token);

        HttpResponseMessage response = await IdentityApi.ResetPasswordAsync(browser.Client, email, token, NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertPasswordUnchangedAsync(browser.Client, email);
    }

    [Fact]
    public async Task ResetPassword_ConsumedToken_Returns400AndDoesNotResetAgain()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.PostJsonAsync(browser.Client, "auth/forgot-password", new { email });
        string token = IdentityApi.ResetTokenFor(email);
        await IdentityApi.ResetPasswordAsync(browser.Client, email, token, NewPassword);

        HttpResponseMessage reuse = await IdentityApi.ResetPasswordAsync(browser.Client, email, token, "Third0SecretPw");

        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityApi.LoginRawAsync(browser.Client, email, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WeakNewPassword_Returns400AndLeavesTheTokenUsable()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.PostJsonAsync(browser.Client, "auth/forgot-password", new { email });
        string token = IdentityApi.ResetTokenFor(email);

        HttpResponseMessage weak = await IdentityApi.ResetPasswordAsync(browser.Client, email, token, "weak");

        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        await AssertPasswordUnchangedAsync(browser.Client, email);
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityApi.ResetPasswordAsync(browser.Client, email, token, NewPassword)).StatusCode);
    }

    private static async Task AssertPasswordUnchangedAsync(HttpClient client, string email)
    {
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityApi.LoginRawAsync(client, email)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await IdentityApi.LoginRawAsync(client, email, NewPassword)).StatusCode);
    }
}
