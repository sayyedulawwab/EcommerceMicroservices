using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.Login;

internal sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    AuthTokenIssuer authTokenIssuer,
    IOptions<AccountOptions> accountOptions)
    : ICommandHandler<LoginCommand, AuthTokens>
{
    public async Task<Result<AuthTokens>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        string email = User.NormalizeEmail(request.Email);

        User? user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

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

        await dbContext.SaveChangesAsync(cancellationToken);

        return tokens;
    }
}
