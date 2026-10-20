using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Domain.Users;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.ConfirmEmail;

internal sealed class ConfirmEmailCommandHandler(
    IUserRepository userRepository,
    IUserTokenRepository userTokenRepository,
    ISecureTokenService secureTokenService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ConfirmEmailCommand>
{
    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByEmail(request.Email, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserTokenErrors.InvalidOrExpired);
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        UserToken? token = await userTokenRepository.GetAsync(
            user.Id,
            UserTokenPurpose.EmailConfirmation,
            secureTokenService.Hash(request.Token),
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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
