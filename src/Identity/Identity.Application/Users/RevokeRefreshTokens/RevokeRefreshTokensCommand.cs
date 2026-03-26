using SharedKernel.Messaging;

namespace Identity.Application.Users.RevokeRefreshTokens;
public record RevokeRefreshTokensCommand(long UserId) : ICommand<long>;