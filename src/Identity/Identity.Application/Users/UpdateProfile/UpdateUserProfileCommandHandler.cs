using Identity.Application.Abstractions.Clock;
using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.UpdateProfile;

internal sealed class UpdateUserProfileCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateUserProfileCommand>
{
    public async Task<Result> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound);
        }

        user.UpdateProfile(request.FirstName, request.LastName, dateTimeProvider.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
