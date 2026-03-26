using SharedKernel.Messaging;

namespace Identity.Application.Users.Login;

public record LoginUserQuery(string Email, string Password) : IQuery<TokenResponse>;