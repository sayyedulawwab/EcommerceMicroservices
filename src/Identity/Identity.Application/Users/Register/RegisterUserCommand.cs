using Identity.Application.Abstractions.Messaging;

namespace Identity.Application.Users.Register;

public record RegisterUserCommand(string FirstName, string LastName, string Email, string Password) : ICommand<long>;