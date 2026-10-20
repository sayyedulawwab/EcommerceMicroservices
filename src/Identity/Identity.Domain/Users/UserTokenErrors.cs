using SharedKernel.Domain;

namespace Identity.Domain.Users;

public static class UserTokenErrors
{
    public static readonly Error InvalidOrExpired = Error.BadRequest(
        "UserToken.InvalidOrExpired",
        "The link is invalid or has expired");
}
