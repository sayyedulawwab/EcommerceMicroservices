using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Auth;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.GetProfile;

internal sealed class GetUserProfileQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetUserProfileQuery, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserProfileResponse>(UserErrors.NotFound);
        }

        return new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailConfirmed = user.EmailConfirmed,
            Roles = user.IsAdmin ? [Roles.Admin] : [],
            CreatedOnUtc = user.CreatedOnUtc
        };
    }
}
