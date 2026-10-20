using Identity.Application.Abstractions.Clock;
using Identity.Domain.Users;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.UpdateProfile;

internal sealed class UpdateUserProfileCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateUserProfileCommand>
{
    public async Task<Result> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound);
        }

        user.UpdateProfile(request.FirstName, request.LastName, dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
