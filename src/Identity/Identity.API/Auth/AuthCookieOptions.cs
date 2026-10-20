namespace Identity.API.Auth;

public sealed class AuthCookieOptions
{
    public const string SectionName = "AuthCookies";

    /// <summary>Cookie domain; leave empty to scope the cookies to the host that set them (the gateway).</summary>
    public string? Domain { get; set; }

    /// <summary>"None" for a frontend on another site (requires Secure), "Lax" or "Strict" for a same-site one.</summary>
    public SameSiteMode SameSite { get; set; } = SameSiteMode.None;

    /// <summary>
    /// Path the refresh token cookie is sent to. "/" works everywhere; narrow it to the refresh/logout URL as the
    /// browser sees it (e.g. "/api/identity/v1/auth" behind the gateway) to keep it out of other requests.
    /// </summary>
    public string RefreshTokenPath { get; set; } = "/";
}
