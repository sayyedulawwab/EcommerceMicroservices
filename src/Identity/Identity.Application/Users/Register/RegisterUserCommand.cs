using Identity.Application.Abstractions.Messaging;

namespace Identity.Application.Users.Register;

public record RegisterUserCommand(string firstName, string lastName, string email, string password) : ICommand<long>;