using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.Register;

internal sealed class RegisterUserCommandHandler(
    IPasswordHasher passwordHasher,
    IApplicationDbContext dbContext,
    UserTokenIssuer userTokenIssuer,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        string email = User.NormalizeEmail(request.Email);

        if (await dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return Result.Failure<Guid>(UserErrors.AlreadyExists);
        }

        string passwordSalt = passwordHasher.GenerateSalt();
        string hashedPassword = passwordHasher.Hash(request.Password, passwordSalt);

        var user = User.Create(
            request.FirstName,
            request.LastName,
            request.Email,
            hashedPassword,
            false,
            dateTimeProvider.UtcNow);

        dbContext.Users.Add(user);

        userTokenIssuer.Issue(user, UserTokenPurpose.EmailConfirmation);

        await dbContext.SaveChangesAsync(cancellationToken);

        return user.Id.Value;
    }
}
