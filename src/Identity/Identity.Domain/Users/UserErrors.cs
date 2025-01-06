using SharedKernel.Domain;

namespace Identity.Domain.Users;
public static class UserErrors
{
    public static readonly Error AlreadyExists = Error.Conflict(
        "User.AlreadyExists",
        "User with provided email already exists");

    public static readonly Error NotFound = Error.NotFound(
        "User.Found",
        "The user with the specified identifier was not found");

    public static readonly Error InvalidCredentials = Error.Conflict(
        "User.InvalidCredentials",
        "The provided credentials were invalid");

}