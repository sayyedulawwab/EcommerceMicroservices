using Identity.Application.Abstractions.Auth;
using Identity.Domain.Users;
using Microsoft.Extensions.Options;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.Login;

internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    AuthTokenIssuer authTokenIssuer,
    IUnitOfWork unitOfWork,
    IOptions<AccountOptions> accountOptions)
    : ICommandHandler<LoginCommand, AuthTokens>
{
    public async Task<Result<AuthTokens>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByEmail(request.Email, cancellationToken);

        // Unknown email and wrong password give the same answer, so accounts can't be enumerated.
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthTokens>(UserErrors.InvalidCredentials);
        }

        if (accountOptions.Value.RequireConfirmedEmail && !user.EmailConfirmed)
        {
            return Result.Failure<AuthTokens>(UserErrors.EmailNotConfirmed);
        }

        AuthTokens tokens = authTokenIssuer.Issue(user, Guid.NewGuid());

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return tokens;
    }
}
