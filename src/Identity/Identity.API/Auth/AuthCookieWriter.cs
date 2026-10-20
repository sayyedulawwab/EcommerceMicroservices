using Identity.Application.Users;
using Microsoft.Extensions.Options;
using SharedKernel.Auth;

namespace Identity.API.Auth;

/// <summary>Puts the tokens into HttpOnly cookies so frontend JavaScript never sees or handles them.</summary>
public sealed class AuthCookieWriter(IOptions<AuthCookieOptions> options)
{
    public void Write(HttpResponse response, AuthTokens tokens)
    {
        response.Cookies.Append(AuthCookies.AccessToken, tokens.AccessToken, CreateOptions("/", tokens.AccessTokenExpiresOnUtc));
        response.Cookies.Append(AuthCookies.RefreshToken, tokens.RefreshToken, CreateOptions(options.Value.RefreshTokenPath, tokens.RefreshTokenExpiresOnUtc));
    }

    public void Clear(HttpResponse response)
    {
        response.Cookies.Delete(AuthCookies.AccessToken, CreateOptions("/", null));
        response.Cookies.Delete(AuthCookies.RefreshToken, CreateOptions(options.Value.RefreshTokenPath, null));
    }

    private CookieOptions CreateOptions(string path, DateTime? expiresOnUtc)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // required by SameSite=None; browsers also accept Secure cookies on http://localhost
            SameSite = options.Value.SameSite,
            Domain = string.IsNullOrWhiteSpace(options.Value.Domain) ? null : options.Value.Domain,
            Path = path,
            Expires = expiresOnUtc is null ? null : new DateTimeOffset(expiresOnUtc.Value, TimeSpan.Zero),
            IsEssential = true
        };
    }
}
