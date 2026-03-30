using Asp.Versioning;
using Identity.API.Extensions;
using Identity.Application.Users.Login;
using Identity.Application.Users.Register;
using Identity.Application.Users.RevokeRefreshTokens;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.API.Controllers.Users;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/auth")]
[ApiController]
public class UsersController : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        ICommandHandler<RegisterUserCommand, long> handler,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password);

        Result<long> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpPost("login")]
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

    [HttpPost("login/refresh-token")]
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


    [HttpDelete("{:userId}/refresh-tokens")]
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
