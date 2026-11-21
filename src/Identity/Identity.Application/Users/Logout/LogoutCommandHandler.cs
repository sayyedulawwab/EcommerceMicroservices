using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.Logout;

/// <summary>Ends the current session: every refresh token of its family stops working. Other devices stay signed in.</summary>
internal sealed class LogoutCommandHandler(
    IApplicationDbContext dbContext,
    ISecureTokenService secureTokenService,
    TimeProvider timeProvider)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> HandleAsync(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result.Success();
        }

        string tokenHash = secureTokenService.Hash(request.RefreshToken);

        RefreshToken? refreshToken = await dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is not null)
        {
            DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

            await dbContext.RefreshTokens
                .Where(r => r.FamilyId == refreshToken.FamilyId && r.RevokedOnUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.RevokedOnUtc, utcNow), cancellationToken);
        }

        return Result.Success();
    }
}
