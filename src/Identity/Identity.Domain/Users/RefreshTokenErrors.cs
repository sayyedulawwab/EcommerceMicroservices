using SharedKernel.Domain;

namespace Identity.Domain.Users;

public static class RefreshTokenErrors
{
    public static readonly Error Expired = Error.BadRequest(
        "RefreshTokne.Expired",
        "THe refresh token is expired");
}