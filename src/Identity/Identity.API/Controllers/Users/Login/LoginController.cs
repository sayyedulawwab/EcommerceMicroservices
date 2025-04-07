using Asp.Versioning;
using Identity.API.Extensions;
using Identity.Application.Users.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Identity.API.Controllers.Users.Login;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/auth/login")]
[ApiController]
public class LoginController(ISender sender) : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpPost]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var query = new LoginUserQuery(request.Email, request.Password);

        Result<TokenResponse> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [MapToApiVersion(1)]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> LoginWithRefreshToken(
        LoginWithRefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var query = new LoginUserWithRefreshTokenQuery(request.RefreshToken);

        Result<TokenResponse> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
