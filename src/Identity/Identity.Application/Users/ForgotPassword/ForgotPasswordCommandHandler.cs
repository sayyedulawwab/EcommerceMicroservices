using Identity.Application.Abstractions.Clock;
using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.ForgotPassword;

/// <summary>Always succeeds, whether or not the email belongs to an account, so accounts can't be enumerated.</summary>
internal sealed class ForgotPasswordCommandHandler(
    IApplicationDbContext dbContext,
    UserTokenIssuer userTokenIssuer,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        string email = User.NormalizeEmail(request.Email);

        User? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            return Result.Success();
        }

        // Old links die only if the new one is saved too.
        await dbContext.ExecuteInTransactionAsync(async ct =>
        {
            DateTime utcNow = dateTimeProvider.UtcNow;

            await dbContext.UserTokens
                .Where(t => t.UserId == user.Id && t.Purpose == UserTokenPurpose.PasswordReset && t.ConsumedOnUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.ConsumedOnUtc, utcNow), ct);

            userTokenIssuer.Issue(user, UserTokenPurpose.PasswordReset);

            await dbContext.SaveChangesAsync(ct);
        }, cancellationToken);

        return Result.Success();
    }
}
