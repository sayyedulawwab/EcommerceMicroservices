using SharedKernel.Messaging;

namespace Identity.Application.Users.RefreshTokens;

public record RefreshTokensCommand(string? RefreshToken) : ICommand<AuthTokens>;
