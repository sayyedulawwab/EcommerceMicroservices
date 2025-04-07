using Identity.Application.Abstractions.Messaging;

namespace Identity.Application.Users.Login;

public record LoginUserQuery(string Email, string Password) : IQuery<TokenResponse>;