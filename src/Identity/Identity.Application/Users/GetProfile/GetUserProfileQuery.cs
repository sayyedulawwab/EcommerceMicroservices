using SharedKernel.Messaging;

namespace Identity.Application.Users.GetProfile;

public record GetUserProfileQuery(long UserId) : IQuery<UserProfileResponse>;
