using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.RefreshTokens;

internal sealed class RefreshTokensCommandHandler(
    IApplicationDbContext dbContext,
    ISecureTokenService secureTokenService,
    AuthTokenIssuer authTokenIssuer,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RefreshTokensCommand, AuthTokens>
{
    // Two browser tabs refreshing at the same moment both present the same token; the slower one must not be
    // mistaken for a stolen token being replayed.
    private static readonly TimeSpan ConcurrentRefreshGracePeriod = TimeSpan.FromSeconds(30);

    public async Task<Result<AuthTokens>> Handle(RefreshTokensCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result.Failure<AuthTokens>(RefreshTokenErrors.Invalid);
        }

        string tokenHash = secureTokenService.Hash(request.RefreshToken);

        RefreshToken? refreshToken = await dbContext.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return Result.Failure<AuthTokens>(RefreshTokenErrors.Invalid);
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        if (refreshToken.IsRevoked)
        {
            if (refreshToken.RevokedOnUtc > utcNow - ConcurrentRefreshGracePeriod)
            {
                return Result.Failure<AuthTokens>(RefreshTokenErrors.Invalid);
            }

            // A rotated-out token came back: assume it was stolen and sign the whole session out.
            await dbContext.RefreshTokens
                .Where(r => r.FamilyId == refreshToken.FamilyId && r.RevokedOnUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.RevokedOnUtc, utcNow), cancellationToken);

            return Result.Failure<AuthTokens>(RefreshTokenErrors.ReuseDetected);
        }

        if (refreshToken.IsExpired(utcNow))
        {
            return Result.Failure<AuthTokens>(RefreshTokenErrors.Invalid);
        }

        refreshToken.Revoke(utcNow);

        AuthTokens tokens = authTokenIssuer.Issue(refreshToken.User, refreshToken.FamilyId);

        await dbContext.SaveChangesAsync(cancellationToken);

        return tokens;
    }
}
