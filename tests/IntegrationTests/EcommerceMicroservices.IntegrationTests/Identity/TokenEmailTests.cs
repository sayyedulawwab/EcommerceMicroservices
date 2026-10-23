using EcommerceMicroservices.IntegrationTests.Infrastructure;
using System.Net;

namespace EcommerceMicroservices.IntegrationTests.Identity;

[Collection(SystemCollection.Name)]
public sealed class TokenEmailTests(SystemFixture system)
{
    private readonly IdentityApi _api = new(system);

    [Fact]
    public async Task Register_MailsOneConfirmationEmailWithTheConfirmLink()
    {
        using BrowserSession browser = _api.NewBrowser();

        string email = await IdentityApi.RegisterAsync(browser.Client);

        CapturedEmail mail = Assert.Single(CapturingEmailSender.To(email));
        Assert.Equal("Confirm your email address", mail.Subject);
        Assert.Contains("Hi Test,", mail.Body, StringComparison.Ordinal);
        Assert.Contains(
            $"http://localhost:3000/confirm-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(mail.Token)}",
            mail.Body,
            StringComparison.Ordinal);
        Assert.Contains("expires in 24 hours", mail.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ForgotPassword_MailsOneResetEmailWithTheResetLink()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);

        await IdentityApi.PostJsonAsync(browser.Client, "auth/forgot-password", new { email });

        CapturedEmail mail = Assert.Single(CapturingEmailSender.To(email), m => m.Subject == "Reset your password");
        Assert.Contains(
            $"http://localhost:3000/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(mail.Token)}",
            mail.Body,
            StringComparison.Ordinal);
        Assert.Contains("expires in 60 minutes", mail.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_BaseUrlWithTrailingSlash_DoesNotProduceADoubleSlash()
    {
        using BrowserSession browser = _api.NewBrowser();

        string email;
        using (_api.OverrideAccountOptions(options => options.FrontendBaseUrl = "https://shop.example.com/"))
        {
            email = await IdentityApi.RegisterAsync(browser.Client);
        }

        CapturedEmail mail = Assert.Single(CapturingEmailSender.To(email));
        Assert.Contains("https://shop.example.com/confirm-email?", mail.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("com//", mail.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FailedRegistration_SendsNoEmail()
    {
        using BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);

        HttpResponseMessage duplicate = await IdentityApi.RegisterRawAsync(browser.Client, email);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Single(CapturingEmailSender.To(email));
    }
}
