using Identity.Domain.Users;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.RevokeRefreshTokens;

internal sealed class RevokeRefreshTokensCommandHandler(IRefreshTokenRepository refreshTokenRepository) : ICommandHandler<RevokeRefreshTokensCommand, long>
{
    public async Task<Result<long>> Handle(RevokeRefreshTokensCommand request, CancellationToken cancellationToken)
    {
        await refreshTokenRepository.DeleteByUserIdAsync(request.UserId);

        return request.UserId;
    }
}