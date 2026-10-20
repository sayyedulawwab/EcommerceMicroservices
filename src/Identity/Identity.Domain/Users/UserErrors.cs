using SharedKernel.Domain;

namespace Identity.Domain.Users;

public static class UserErrors
{
    public static readonly Error AlreadyExists = Error.Conflict(
        "User.AlreadyExists",
        "User with provided email already exists");

    public static readonly Error NotFound = Error.NotFound(
        "User.NotFound",
        "The user with the specified identifier was not found");

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "User.InvalidCredentials",
        "The provided credentials were invalid");

    public static readonly Error EmailNotConfirmed = Error.Forbidden(
        "User.EmailNotConfirmed",
        "The email address has not been confirmed yet");

    public static readonly Error InvalidCurrentPassword = Error.BadRequest(
        "User.InvalidCurrentPassword",
        "The current password is not correct");

}
