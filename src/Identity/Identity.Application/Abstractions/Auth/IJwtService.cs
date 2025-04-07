using SharedKernel.Domain;

namespace Identity.Application.Abstractions.Auth;
public interface IJwtService
{
    string GetAccessToken(string email, long userId, CancellationToken cancellationToken = default);
    string GenerateRefreshToken();
}
