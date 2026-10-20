using SharedKernel.Messaging;

namespace Identity.Application.Users.Login;

public record LoginCommand(string Email, string Password) : ICommand<AuthTokens>;
