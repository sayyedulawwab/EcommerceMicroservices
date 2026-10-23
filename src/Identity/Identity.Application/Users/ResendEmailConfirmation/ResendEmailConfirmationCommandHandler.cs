using Identity.Application.Abstractions.Clock;
using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.ResendEmailConfirmation;

/// <summary>Always succeeds, whether or not the email belongs to an account, so accounts can't be enumerated.</summary>
internal sealed class ResendEmailConfirmationCommandHandler(
    IApplicationDbContext dbContext,
    UserTokenIssuer userTokenIssuer,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ResendEmailConfirmationCommand>
{
    public async Task<Result> Handle(ResendEmailConfirmationCommand request, CancellationToken cancellationToken)
    {
        string email = User.NormalizeEmail(request.Email);

        User? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || user.EmailConfirmed)
        {
            return Result.Success();
        }

        // Old links die only if the new one is saved too.
        await dbContext.ExecuteInTransactionAsync(async ct =>
        {
            DateTime utcNow = dateTimeProvider.UtcNow;

            await dbContext.UserTokens
                .Where(t => t.UserId == user.Id && t.Purpose == UserTokenPurpose.EmailConfirmation && t.ConsumedOnUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.ConsumedOnUtc, utcNow), ct);

            userTokenIssuer.Issue(user, UserTokenPurpose.EmailConfirmation);

            await dbContext.SaveChangesAsync(ct);
        }, cancellationToken);

        return Result.Success();
    }
}
