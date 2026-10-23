using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.ConfirmEmail;

internal sealed class ConfirmEmailCommandHandler(
    IApplicationDbContext dbContext,
    ISecureTokenService secureTokenService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ConfirmEmailCommand>
{
    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        string email = User.NormalizeEmail(request.Email);

        User? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserTokenErrors.InvalidOrExpired);
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        string tokenHash = secureTokenService.Hash(request.Token);

        UserToken? token = await dbContext.UserTokens.FirstOrDefaultAsync(
            t => t.UserId == user.Id && t.Purpose == UserTokenPurpose.EmailConfirmation && t.TokenHash == tokenHash,
            cancellationToken);

        // Check the token before anything else, so the answer never reveals whether an address has an account.
        if (token is null)
        {
            return Result.Failure(UserTokenErrors.InvalidOrExpired);
        }

        // Clicking the same link twice is fine.
        if (user.EmailConfirmed)
        {
            return Result.Success();
        }

        if (!token.IsUsable(utcNow))
        {
            return Result.Failure(UserTokenErrors.InvalidOrExpired);
        }

        token.Consume(utcNow);
        user.ConfirmEmail(utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
