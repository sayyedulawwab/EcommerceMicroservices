using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    IApplicationDbContext dbContext,
    ISecureTokenService secureTokenService,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
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
            t => t.UserId == user.Id && t.Purpose == UserTokenPurpose.PasswordReset && t.TokenHash == tokenHash,
            cancellationToken);

        if (token is null || !token.IsUsable(utcNow))
        {
            return Result.Failure(UserTokenErrors.InvalidOrExpired);
        }

        token.Consume(utcNow);

        user.ChangePasswordHash(passwordHasher.Hash(request.NewPassword, passwordHasher.GenerateSalt()), utcNow);

        // The reset link reached the inbox, which proves the address too.
        user.ConfirmEmail(utcNow);

        // Whoever knew the old password may still hold a session: end them all, atomically with the new password.
        await dbContext.ExecuteInTransactionAsync(async ct =>
        {
            await dbContext.RefreshTokens
                .Where(r => r.UserId == user.Id && r.RevokedOnUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.RevokedOnUtc, utcNow), ct);

            await dbContext.SaveChangesAsync(ct);
        }, cancellationToken);

        return Result.Success();
    }
}
