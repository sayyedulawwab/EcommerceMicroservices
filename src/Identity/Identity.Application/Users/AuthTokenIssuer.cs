using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Domain.Users;
using Microsoft.Extensions.Options;

namespace Identity.Application.Users;

internal sealed class AuthTokenIssuer(
    IJwtService jwtService,
    ISecureTokenService secureTokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IDateTimeProvider dateTimeProvider,
    IOptions<AccountOptions> accountOptions)
{
    /// <summary>
    /// Creates an access token and a new refresh token in <paramref name="familyId"/> (a new family starts a new
    /// session). The refresh token is only added to the repository; the caller saves.
    /// </summary>
    public AuthTokens Issue(User user, Guid familyId)
    {
        DateTime utcNow = dateTimeProvider.UtcNow;

        AccessToken accessToken = jwtService.CreateAccessToken(user);

        string rawRefreshToken = secureTokenService.Generate();
        DateTime refreshTokenExpiresOnUtc = utcNow.AddDays(accountOptions.Value.RefreshTokenLifetimeDays);

        refreshTokenRepository.Add(RefreshToken.Create(
            secureTokenService.Hash(rawRefreshToken),
            user.Id,
            familyId,
            refreshTokenExpiresOnUtc,
            utcNow));

        return new AuthTokens(accessToken.Token, accessToken.ExpiresOnUtc, rawRefreshToken, refreshTokenExpiresOnUtc);
    }
}
