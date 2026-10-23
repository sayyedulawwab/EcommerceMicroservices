using EcommerceMicroservices.IntegrationTests.Infrastructure;
using Identity.Domain.Users;
using System.Net;

namespace EcommerceMicroservices.IntegrationTests.Identity;

[Collection(SystemCollection.Name)]
public sealed class RegistrationTests(SystemFixture system)
{
    private readonly IdentityApi _api = new(system);

    [Fact]
    public async Task Register_NewEmail_StoresUserWithHashedPasswordAndNormalizedEmail()
    {
        using BrowserSession browser = _api.NewBrowser();
        string id = Guid.NewGuid().ToString("N");
        string typedEmail = $"Mixed.Case-{id}@Test.Local";
        string storedEmail = $"mixed.case-{id}@test.local";

        HttpResponseMessage response = await IdentityApi.RegisterRawAsync(browser.Client, typedEmail);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        User user = await _api.GetUserAsync(storedEmail);

        Assert.Equal(storedEmail, user.Email);
        Assert.False(user.EmailConfirmed);
        Assert.False(user.IsAdmin);
        Assert.NotEqual(IdentityApi.Password, user.PasswordHash);
        Assert.DoesNotContain(IdentityApi.Password, user.PasswordHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_NewEmail_IssuesOneEmailConfirmationTokenAndMailsItsLink()
    {
        using BrowserSession browser = _api.NewBrowser();

        string email = await IdentityApi.RegisterAsync(browser.Client);

        User user = await _api.GetUserAsync(email);
        List<UserToken> tokens = await _api.UserTokensAsync(user.Id, UserTokenPurpose.EmailConfirmation);

        UserToken token = Assert.Single(tokens);
        Assert.Null(token.ConsumedOnUtc);
        Assert.True(token.ExpiresOnUtc > DateTime.UtcNow);

        CapturedEmail mail = Assert.Single(CapturingEmailSender.To(email));
        Assert.Equal(_api.Hash(mail.Token), token.TokenHash);
    }

    [Theory]
    [InlineData("same")]
    [InlineData("UPPER")]
    public async Task Register_EmailAlreadyRegistered_Returns409AndCreatesNothing(string variant)
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        string duplicate = variant == "UPPER" ? email.ToUpperInvariant() : email;

        HttpResponseMessage response = await IdentityApi.RegisterRawAsync(browser.Client, duplicate);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await _api.CountUsersAsync(email));

        User user = await _api.GetUserAsync(email);
        Assert.Single(await _api.UserTokensAsync(user.Id, UserTokenPurpose.EmailConfirmation));
        Assert.Single(CapturingEmailSender.To(email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ab1defg")]
    [InlineData("alllower1")]
    [InlineData("ALLUPPER1")]
    [InlineData("NoDigitsHere")]
    public async Task Register_WeakPassword_Returns400AndCreatesNothing(string password)
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = IdentityApi.NewEmail();

        HttpResponseMessage response = await IdentityApi.RegisterRawAsync(browser.Client, email, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await _api.CountUsersAsync(email));
        Assert.Empty(CapturingEmailSender.To(email));
    }

    [Fact]
    public async Task Register_PasswordOfExactlyEightCharacters_Succeeds()
    {
        using BrowserSession browser = _api.NewBrowser();

        HttpResponseMessage response = await IdentityApi.RegisterRawAsync(browser.Client, IdentityApi.NewEmail(), "Abcdef1g");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("jane@")]
    public async Task Register_InvalidEmail_Returns400(string email)
    {
        using BrowserSession browser = _api.NewBrowser();

        HttpResponseMessage response = await IdentityApi.RegisterRawAsync(browser.Client, email);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await _api.CountUsersAsync(email));
    }

    [Fact]
    public async Task Register_EmptyNames_Returns400AndCreatesNothing()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = IdentityApi.NewEmail();

        HttpResponseMessage response = await IdentityApi.RegisterRawAsync(browser.Client, email, firstName: "", lastName: "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await _api.CountUsersAsync(email));
    }
}
