using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Application.Abstractions.Messaging;
using Identity.Domain.Users;
using SharedKernel.Domain;

namespace Identity.Application.Users.Register;
internal sealed class RegisterUserCommandHandler(
    IPasswordHasher passwordHasher,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RegisterUserCommand, long>
{
    public async Task<Result<long>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        User? existingUserByEmail = await userRepository.GetByEmail(request.Email);

        if (existingUserByEmail is not null)
        {
            return Result.Failure<long>(UserErrors.AlreadyExists);
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

        userRepository.Add(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}