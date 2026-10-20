namespace Identity.Application.Users;

/// <summary>Never returned in a response body: the API writes these into HttpOnly cookies.</summary>
public sealed record AuthTokens(
    string AccessToken,
    DateTime AccessTokenExpiresOnUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresOnUtc);
