using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;

namespace EcommerceMicroservices.IntegrationTests.Infrastructure;

/// <summary>
/// Shared steps for the Identity tests: the HTTP calls a user makes, plus read access to Identity's database so a
/// test can check what was (or was not) persisted and backdate a token to test expiry.
/// </summary>
public sealed class IdentityApi(SystemFixture system)
{
    public const string Password = "Sup3rSecretPw";
    public const string RequestedWithHeader = "X-Requested-With";

    private const string BaseUrl = "/api/v1";

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static string NewEmail() => $"user-{Guid.NewGuid():N}@test.local";

    public BrowserSession NewBrowser() => system.CreateBrowserSession();

    // ---- HTTP ----

    public static async Task<HttpResponseMessage> RegisterRawAsync(HttpClient client, string email, string password = Password, string firstName = "Test", string lastName = "User")
    {
        return await client.PostAsJsonAsync(
            $"{BaseUrl}/auth/register",
            new { firstName, lastName, email, password },
            Ct);
    }

    public static async Task<string> RegisterAsync(HttpClient client, string? email = null, string password = Password)
    {
        email ??= NewEmail();

        HttpResponseMessage response = await RegisterRawAsync(client, email, password);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return email;
    }

    public static async Task<HttpResponseMessage> LoginRawAsync(HttpClient client, string email, string password = Password)
    {
        return await client.PostAsJsonAsync($"{BaseUrl}/auth/login", new { email, password }, Ct);
    }

    public static async Task LoginAsync(HttpClient client, string email, string password = Password)
    {
        HttpResponseMessage response = await LoginRawAsync(client, email, password);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public static async Task<HttpResponseMessage> PostWithoutBodyAsync(HttpClient client, string route)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{route}");
        request.Headers.Add(RequestedWithHeader, "XMLHttpRequest");

        return await client.SendAsync(request, Ct);
    }

    /// <summary>Sends POST /auth/refresh or /auth/logout carrying only this refresh token cookie (no cookie jar).</summary>
    public async Task<HttpResponseMessage> PostWithRefreshCookieAsync(string route, string? refreshToken)
    {
        using HttpClient client = system.CreateCookielessIdentityClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{route}");
        request.Headers.Add(RequestedWithHeader, "XMLHttpRequest");

        if (refreshToken is not null)
        {
            request.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        }

        return await client.SendAsync(request, Ct);
    }

    public Task<HttpResponseMessage> RefreshAsync(string? refreshToken) => PostWithRefreshCookieAsync("auth/refresh", refreshToken);

    public static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string route, object body)
    {
        return await client.PostAsJsonAsync($"{BaseUrl}/{route}", body, Ct);
    }

    public static async Task<HttpResponseMessage> ConfirmEmailAsync(HttpClient client, string email, string token) =>
        await PostJsonAsync(client, "auth/confirm-email", new { email, token });

    public static async Task<HttpResponseMessage> ResetPasswordAsync(HttpClient client, string email, string token, string newPassword) =>
        await PostJsonAsync(client, "auth/reset-password", new { email, token, newPassword });

    /// <summary>The access token cookie is not enough to know the session works; this is what the frontend calls.</summary>
    public static async Task<HttpStatusCode> MeStatusAsync(HttpClient client) =>
        (await client.GetAsync($"{BaseUrl}/users/me", Ct)).StatusCode;

    // ---- Database ----

    public async Task<T> QueryAsync<T>(Func<IApplicationDbContext, Task<T>> query)
    {
        await using AsyncServiceScope scope = system.Identity.Services.CreateAsyncScope();

        return await query(scope.ServiceProvider.GetRequiredService<IApplicationDbContext>());
    }

    public string Hash(string rawToken)
    {
        using IServiceScope scope = system.Identity.Services.CreateScope();

        return scope.ServiceProvider.GetRequiredService<ISecureTokenService>().Hash(rawToken);
    }

    public async Task<User?> FindUserAsync(string email) =>
        await QueryAsync(db => db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, Ct));

    public async Task<User> GetUserAsync(string email) =>
        await FindUserAsync(email) ?? throw new InvalidOperationException($"No user {email}");

    public async Task<int> CountUsersAsync(string email) =>
        await QueryAsync(db => db.Users.CountAsync(u => u.Email == email, Ct));

    public async Task<List<RefreshToken>> RefreshTokensAsync(UserId userId) =>
        await QueryAsync(db => db.RefreshTokens.AsNoTracking().Where(r => r.UserId == userId).OrderBy(r => EF.Property<long>(r, "ClusterId")).ToListAsync(Ct));

    public async Task<RefreshToken?> FindRefreshTokenAsync(string rawToken)
    {
        string hash = Hash(rawToken);

        return await QueryAsync(db => db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(r => r.TokenHash == hash, Ct));
    }

    public async Task<List<UserToken>> UserTokensAsync(UserId userId, UserTokenPurpose purpose) =>
        await QueryAsync(db => db.UserTokens.AsNoTracking()
            .Where(t => t.UserId == userId && t.Purpose == purpose)
            .OrderBy(t => EF.Property<long>(t, "ClusterId"))
            .ToListAsync(Ct));

    public async Task SetRefreshTokenTimesAsync(string rawToken, DateTime? expiresOnUtc = null, DateTime? revokedOnUtc = null)
    {
        string hash = Hash(rawToken);

        await QueryAsync(async db =>
        {
            if (expiresOnUtc is not null)
            {
                await db.RefreshTokens.Where(r => r.TokenHash == hash)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.ExpiresOnUtc, expiresOnUtc.Value), Ct);
            }

            if (revokedOnUtc is not null)
            {
                await db.RefreshTokens.Where(r => r.TokenHash == hash)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedOnUtc, revokedOnUtc), Ct);
            }

            return 0;
        });
    }

    public async Task ExpireUserTokenAsync(string rawToken)
    {
        string hash = Hash(rawToken);
        DateTime past = DateTime.UtcNow.AddMinutes(-5);

        await QueryAsync(db => db.UserTokens.Where(t => t.TokenHash == hash)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresOnUtc, past), Ct));
    }

    /// <summary>
    /// Changes Identity's account settings (the running host's singleton options) until the result is disposed.
    /// Test classes run one after another, so no other test sees the change.
    /// </summary>
    public IDisposable OverrideAccountOptions(Action<AccountOptions> change)
    {
        AccountOptions options = system.Identity.Services.GetRequiredService<IOptions<AccountOptions>>().Value;
        var original = new AccountOptions
        {
            FrontendBaseUrl = options.FrontendBaseUrl,
            RequireConfirmedEmail = options.RequireConfirmedEmail,
            RefreshTokenLifetimeDays = options.RefreshTokenLifetimeDays,
            EmailConfirmationTokenLifetimeHours = options.EmailConfirmationTokenLifetimeHours,
            PasswordResetTokenLifetimeMinutes = options.PasswordResetTokenLifetimeMinutes
        };

        change(options);

        return new Restore(options, original);
    }

    /// <summary>The confirmation token from the latest mailed link.</summary>
    public static string ConfirmationTokenFor(string email) =>
        CapturingEmailSender.To(email).Last(mail => mail.Subject.Contains("Confirm", StringComparison.Ordinal)).Token;

    public static string ResetTokenFor(string email) =>
        CapturingEmailSender.To(email).Last(mail => mail.Subject.Contains("Reset", StringComparison.Ordinal)).Token;

    private sealed class Restore(AccountOptions live, AccountOptions original) : IDisposable
    {
        public void Dispose()
        {
            live.FrontendBaseUrl = original.FrontendBaseUrl;
            live.RequireConfirmedEmail = original.RequireConfirmedEmail;
            live.RefreshTokenLifetimeDays = original.RefreshTokenLifetimeDays;
            live.EmailConfirmationTokenLifetimeHours = original.EmailConfirmationTokenLifetimeHours;
            live.PasswordResetTokenLifetimeMinutes = original.PasswordResetTokenLifetimeMinutes;
        }
    }
}
