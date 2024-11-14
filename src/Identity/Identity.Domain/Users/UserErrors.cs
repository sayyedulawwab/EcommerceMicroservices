using Identity.Domain.Abstractions;

namespace Identity.Domain.Users;
public static class UserErrors
{
    public static Error AlreadyExists = new(
        "User.AlreadyExists",
        "User with provided email already exists");

    public static Error NotFound = new(
        "User.Found",
        "The user with the specified identifier was not found");

    public static Error InvalidCredentials = new(
        "User.InvalidCredentials",
        "The provided credentials were invalid");

}