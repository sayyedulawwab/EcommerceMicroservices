using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.Extensions.Options;

namespace Identity.Application.Users;

internal sealed class AuthTokenIssuer(
    IJwtService jwtService,
    ISecureTokenService secureTokenService,
    IApplicationDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<AccountOptions> accountOptions)
{
    /// <summary>
    /// Creates an access token and a new refresh token in <paramref name="familyId"/> (a new family starts a new
    /// session). The refresh token is only added to the context; the caller saves.
    /// </summary>
    public AuthTokens Issue(User user, Guid familyId)
    {
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        AccessToken accessToken = jwtService.CreateAccessToken(user);

        string rawRefreshToken = secureTokenService.Generate();
        DateTime refreshTokenExpiresOnUtc = utcNow.AddDays(accountOptions.Value.RefreshTokenLifetimeDays);

        dbContext.RefreshTokens.Add(RefreshToken.Create(
            secureTokenService.Hash(rawRefreshToken),
            user.Id,
            familyId,
            refreshTokenExpiresOnUtc,
            utcNow));

        return new AuthTokens(accessToken.Token, accessToken.ExpiresOnUtc, rawRefreshToken, refreshTokenExpiresOnUtc);
    }
}
