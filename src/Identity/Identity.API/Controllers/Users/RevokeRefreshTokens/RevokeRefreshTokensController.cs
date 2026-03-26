using Asp.Versioning;
using Identity.API.Extensions;
using Identity.Application.Users.Register;
using Identity.Application.Users.RevokeRefreshTokens;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.API.Controllers.Users.RevokeRefreshTokens;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/users/{:id}/refresh-tokens")]
[ApiController]
public class RevokeRefreshTokensController() : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpDelete]
    public async Task<IActionResult> RevokeRefreshTokens(
        long userId,
        ICommandHandler<RevokeRefreshTokensCommand, long> handler,
        CancellationToken cancellationToken)
    {
        var command = new RevokeRefreshTokensCommand(userId);

        Result<long> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}