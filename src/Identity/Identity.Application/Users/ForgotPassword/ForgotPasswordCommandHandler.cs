using Identity.Application.Abstractions.Clock;
using Identity.Domain.Users;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.ForgotPassword;

/// <summary>Always succeeds, whether or not the email belongs to an account, so accounts can't be enumerated.</summary>
internal sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IUserTokenRepository userTokenRepository,
    UserTokenIssuer userTokenIssuer,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByEmail(request.Email, cancellationToken);

        if (user is null)
        {
            return Result.Success();
        }

        // Old links die only if the new one is saved too.
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await userTokenRepository.ConsumeAllAsync(user.Id, UserTokenPurpose.PasswordReset, dateTimeProvider.UtcNow, ct);

            userTokenIssuer.Issue(user, UserTokenPurpose.PasswordReset);

            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        return Result.Success();
    }
}
