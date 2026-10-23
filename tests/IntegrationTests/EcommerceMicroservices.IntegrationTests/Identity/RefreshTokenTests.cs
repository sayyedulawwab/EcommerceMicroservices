using EcommerceMicroservices.IntegrationTests.Infrastructure;
using Identity.Domain.Users;
using System.Net;

namespace EcommerceMicroservices.IntegrationTests.Identity;

[Collection(SystemCollection.Name)]
public sealed class RefreshTokenTests(SystemFixture system)
{
    private readonly IdentityApi _api = new(system);

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401()
    {
        HttpResponseMessage response = await _api.RefreshAsync(null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("RefreshToken.Invalid", await response.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Refresh_UnknownToken_Returns401AndClearsTheCookies()
    {
        HttpResponseMessage response = await _api.RefreshAsync("not-a-token-we-issued");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("RefreshToken.Invalid", await response.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("refresh_token=;", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Refresh_ValidToken_RevokesTheOldTokenAndIssuesANewOneInTheSameFamily()
    {
        (BrowserSession browser, string email) = await LoggedInAsync();
        using (browser)
        {
            string oldRaw = browser.GetCookie("refresh_token")!;

            HttpResponseMessage response = await IdentityApi.PostWithoutBodyAsync(browser.Client, "auth/refresh");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            string newRaw = browser.GetCookie("refresh_token")!;
            User user = await _api.GetUserAsync(email);
            List<RefreshToken> tokens = await _api.RefreshTokensAsync(user.Id);

            Assert.Equal(2, tokens.Count);
            RefreshToken old = tokens.Single(t => t.TokenHash == _api.Hash(oldRaw));
            RefreshToken current = tokens.Single(t => t.TokenHash == _api.Hash(newRaw));
            Assert.NotNull(old.RevokedOnUtc);
            Assert.Null(current.RevokedOnUtc);
            Assert.Equal(old.FamilyId, current.FamilyId);
            Assert.Equal(HttpStatusCode.OK, await IdentityApi.MeStatusAsync(browser.Client));
        }
    }

    [Fact]
    public async Task Refresh_ExpiredToken_Returns401AndRotatesNothing()
    {
        (BrowserSession browser, string email) = await LoggedInAsync();
        using (browser)
        {
            string raw = browser.GetCookie("refresh_token")!;
            await _api.SetRefreshTokenTimesAsync(raw, expiresOnUtc: DateTime.UtcNow.AddMinutes(-1));

            HttpResponseMessage response = await _api.RefreshAsync(raw);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("RefreshToken.Invalid", await response.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);

            User user = await _api.GetUserAsync(email);
            RefreshToken stored = Assert.Single(await _api.RefreshTokensAsync(user.Id));
            Assert.Null(stored.RevokedOnUtc);
        }
    }

    [Fact]
    public async Task Refresh_RotatedTokenReplayedWithinGracePeriod_Returns401ButKeepsTheSession()
    {
        (BrowserSession browser, string email) = await LoggedInAsync();
        using (browser)
        {
            string oldRaw = browser.GetCookie("refresh_token")!;
            await IdentityApi.PostWithoutBodyAsync(browser.Client, "auth/refresh");
            string currentRaw = browser.GetCookie("refresh_token")!;

            HttpResponseMessage replay = await _api.RefreshAsync(oldRaw);

            Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
            Assert.Contains("RefreshToken.Invalid", await replay.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);

            // Two tabs refreshing at once must not sign the user out: the newest token still works.
            Assert.Null((await _api.FindRefreshTokenAsync(currentRaw))!.RevokedOnUtc);
            Assert.Equal(HttpStatusCode.NoContent, (await _api.RefreshAsync(currentRaw)).StatusCode);

            User user = await _api.GetUserAsync(email);
            Assert.Equal(3, (await _api.RefreshTokensAsync(user.Id)).Count);
        }
    }

    [Theory]
    [InlineData(31)]
    [InlineData(3600)]
    public async Task Refresh_RotatedTokenReplayedAfterGracePeriod_RevokesTheWholeFamily(int secondsSinceRevocation)
    {
        (BrowserSession browser, string email) = await LoggedInAsync();
        using (browser)
        {
            string oldRaw = browser.GetCookie("refresh_token")!;
            await IdentityApi.PostWithoutBodyAsync(browser.Client, "auth/refresh");
            string currentRaw = browser.GetCookie("refresh_token")!;
            await _api.SetRefreshTokenTimesAsync(oldRaw, revokedOnUtc: DateTime.UtcNow.AddSeconds(-secondsSinceRevocation));

            HttpResponseMessage replay = await _api.RefreshAsync(oldRaw);

            Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
            Assert.Contains("RefreshToken.ReuseDetected", await replay.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);

            User user = await _api.GetUserAsync(email);
            Assert.All(await _api.RefreshTokensAsync(user.Id), token => Assert.NotNull(token.RevokedOnUtc));

            // The thief's replay also ended the legitimate user's newest token.
            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.RefreshAsync(currentRaw)).StatusCode);
        }
    }

    [Fact]
    public async Task Refresh_ReuseDetected_DoesNotTouchOtherSessionsOfTheUser()
    {
        (BrowserSession browser, string email) = await LoggedInAsync();
        using (browser)
        using (BrowserSession otherDevice = _api.NewBrowser())
        {
            await IdentityApi.LoginAsync(otherDevice.Client, email);
            string oldRaw = browser.GetCookie("refresh_token")!;
            await IdentityApi.PostWithoutBodyAsync(browser.Client, "auth/refresh");
            await _api.SetRefreshTokenTimesAsync(oldRaw, revokedOnUtc: DateTime.UtcNow.AddMinutes(-5));

            await _api.RefreshAsync(oldRaw);

            Assert.Equal(HttpStatusCode.NoContent, (await IdentityApi.PostWithoutBodyAsync(otherDevice.Client, "auth/refresh")).StatusCode);
        }
    }

    [Fact]
    public async Task Refresh_RevokedAndExpiredTokenReplayedLater_IsStillTreatedAsReuse()
    {
        (BrowserSession browser, string email) = await LoggedInAsync();
        using (browser)
        {
            string raw = browser.GetCookie("refresh_token")!;
            await _api.SetRefreshTokenTimesAsync(raw, expiresOnUtc: DateTime.UtcNow.AddDays(-1), revokedOnUtc: DateTime.UtcNow.AddDays(-2));

            HttpResponseMessage replay = await _api.RefreshAsync(raw);

            Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
            Assert.Contains("RefreshToken.ReuseDetected", await replay.Content.ReadAsStringAsync(IdentityApi.Ct), StringComparison.Ordinal);
            Assert.NotEmpty(await _api.RefreshTokensAsync((await _api.GetUserAsync(email)).Id));
        }
    }

    private async Task<(BrowserSession Browser, string Email)> LoggedInAsync()
    {
        BrowserSession browser = _api.NewBrowser();
        string email = await IdentityApi.RegisterAsync(browser.Client);
        await IdentityApi.LoginAsync(browser.Client, email);

        return (browser, email);
    }
}
