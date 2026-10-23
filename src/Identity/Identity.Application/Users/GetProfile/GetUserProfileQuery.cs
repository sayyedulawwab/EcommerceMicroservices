using SharedKernel.Messaging;

namespace Identity.Application.Users.GetProfile;

public record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileResponse>;
