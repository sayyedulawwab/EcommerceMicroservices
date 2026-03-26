using SharedKernel.Messaging;

namespace Identity.Application.Users.Login;
public record LoginUserWithRefreshTokenQuery(string RefreshToken) : IQuery<TokenResponse>;