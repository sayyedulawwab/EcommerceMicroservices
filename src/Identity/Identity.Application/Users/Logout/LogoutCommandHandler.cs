using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Domain.Users;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.Logout;

/// <summary>Ends the current session: every refresh token of its family stops working. Other devices stay signed in.</summary>
internal sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ISecureTokenService secureTokenService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result.Success();
        }

        RefreshToken? refreshToken = await refreshTokenRepository.GetByTokenHashAsync(
            secureTokenService.Hash(request.RefreshToken),
            cancellationToken);

        if (refreshToken is not null)
        {
            await refreshTokenRepository.RevokeFamilyAsync(refreshToken.FamilyId, dateTimeProvider.UtcNow, cancellationToken);
        }

        return Result.Success();
    }
}
