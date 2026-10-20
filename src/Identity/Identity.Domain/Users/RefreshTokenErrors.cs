using SharedKernel.Domain;

namespace Identity.Domain.Users;

public static class RefreshTokenErrors
{
    public static readonly Error Invalid = Error.Unauthorized(
        "RefreshToken.Invalid",
        "The refresh token is missing, expired or revoked");

    public static readonly Error ReuseDetected = Error.Unauthorized(
        "RefreshToken.ReuseDetected",
        "The refresh token was already used; the session has been signed out");
}
