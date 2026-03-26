using Asp.Versioning;
using Identity.API.Extensions;
using Identity.Application.Users.Login;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.API.Controllers.Users.Login;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/auth/login")]
[ApiController]
public class LoginController() : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpPost]
    public async Task<IActionResult> Login(
        LoginRequest request,
        IQueryHandler<LoginUserQuery, TokenResponse> handler,
        CancellationToken cancellationToken)
    {
        var query = new LoginUserQuery(request.Email, request.Password);

        Result<TokenResponse> result = await handler.Handle(query, cancellationToken);

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
        IQueryHandler<LoginUserWithRefreshTokenQuery, TokenResponse> handler,
        CancellationToken cancellationToken)
    {
        var query = new LoginUserWithRefreshTokenQuery(request.RefreshToken);

        Result<TokenResponse> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
