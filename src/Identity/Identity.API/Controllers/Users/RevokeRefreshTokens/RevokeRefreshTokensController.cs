using Asp.Versioning;
using Identity.API.Controllers.Users.Login;
using Identity.API.Extensions;
using Identity.Application.Users.Login;
using Identity.Application.Users.RevokeRefreshTokens;
using Identity.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Identity.API.Controllers.Users.RevokeRefreshTokens;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/users/{:id}/refresh-tokens")]
[ApiController]
public class RevokeRefreshTokensController(ISender sender) : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpDelete]
    public async Task<IActionResult> RevokeRefreshTokens(
        long userId,
        CancellationToken cancellationToken)
    {
        var command = new RevokeRefreshTokensCommand(userId);

        Result<long> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}