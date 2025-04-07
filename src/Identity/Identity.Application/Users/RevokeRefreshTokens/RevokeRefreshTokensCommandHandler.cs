using Identity.Application.Abstractions.Messaging;
using Identity.Application.Users.Register;
using Identity.Domain.Users;
using SharedKernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Application.Users.RevokeRefreshTokens;
internal sealed class RevokeRefreshTokensCommandHandler(IRefreshTokenRepository refreshTokenRepository) : ICommandHandler<RevokeRefreshTokensCommand, long>
{
    public async Task<Result<long>> Handle(RevokeRefreshTokensCommand request, CancellationToken cancellationToken)
    {
        await refreshTokenRepository.DeleteByUserIdAsync(request.UserId);

        return request.UserId;
    }
}