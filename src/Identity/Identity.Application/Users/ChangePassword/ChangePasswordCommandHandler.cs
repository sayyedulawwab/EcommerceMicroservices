using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.ChangePassword;

/// <summary>
/// Changes the password and signs out every session, then starts a fresh one for the caller so they stay logged in.
/// </summary>
internal sealed class ChangePasswordCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    AuthTokenIssuer authTokenIssuer,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ChangePasswordCommand, AuthTokens>
{
    public async Task<Result<AuthTokens>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<AuthTokens>(UserErrors.NotFound);
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure<AuthTokens>(UserErrors.InvalidCurrentPassword);
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        user.ChangePasswordHash(passwordHasher.Hash(request.NewPassword, passwordHasher.GenerateSalt()), utcNow);

        AuthTokens tokens = authTokenIssuer.Issue(user, Guid.NewGuid());

        // Revoking runs as an immediate UPDATE; the transaction keeps it from sticking if the save fails.
        await dbContext.ExecuteInTransactionAsync(async ct =>
        {
            await dbContext.RefreshTokens
                .Where(r => r.UserId == user.Id && r.RevokedOnUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.RevokedOnUtc, utcNow), ct);

            await dbContext.SaveChangesAsync(ct);
        }, cancellationToken);

        return tokens;
    }
}
