using Identity.Domain.Users;

namespace Identity.Application.Abstractions.Auth;

public interface IJwtService
{
    AccessToken CreateAccessToken(User user);
}

public sealed record AccessToken(string Token, DateTime ExpiresOnUtc);
