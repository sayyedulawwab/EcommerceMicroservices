using Identity.Application.Abstractions.Messaging;

namespace Identity.Application.Users.Login;

public record LoginUserQuery(string email, string password) : IQuery<AccessTokenResponse>;