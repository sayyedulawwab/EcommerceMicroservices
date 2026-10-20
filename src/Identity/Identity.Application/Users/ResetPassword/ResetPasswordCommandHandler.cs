using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Domain.Users;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IUserTokenRepository userTokenRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ISecureTokenService secureTokenService,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByEmail(request.Email, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserTokenErrors.InvalidOrExpired);
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        UserToken? token = await userTokenRepository.GetAsync(
            user.Id,
            UserTokenPurpose.PasswordReset,
            secureTokenService.Hash(request.Token),
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
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await refreshTokenRepository.RevokeAllForUserAsync(user.Id, utcNow, ct);

            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        return Result.Success();
    }
}
