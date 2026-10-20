using Asp.Versioning;
using Identity.API.Auth;
using Identity.API.Extensions;
using Identity.Application.Users;
using Identity.Application.Users.ChangePassword;
using Identity.Application.Users.GetProfile;
using Identity.Application.Users.UpdateProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.API.Controllers.Users;

/// <summary>The signed-in user's own account.</summary>
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/users/me")]
[ApiController]
[Authorize]
public class UsersController(AuthCookieWriter authCookieWriter) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfile(
        IQueryHandler<GetUserProfileQuery, UserProfileResponse> handler,
        CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not long userId)
        {
            return Unauthorized();
        }

        Result<UserProfileResponse> result = await handler.Handle(new GetUserProfileQuery(userId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile(
        UpdateProfileRequest request,
        ICommandHandler<UpdateUserProfileCommand> handler,
        CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not long userId)
        {
            return Unauthorized();
        }

        Result result = await handler.Handle(
            new UpdateUserProfileCommand(userId, request.FirstName, request.LastName),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return NoContent();
    }

    /// <summary>Changes the password, signs out every other session and renews this one's cookies.</summary>
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        ICommandHandler<ChangePasswordCommand, AuthTokens> handler,
        CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not long userId)
        {
            return Unauthorized();
        }

        Result<AuthTokens> result = await handler.Handle(
            new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        authCookieWriter.Write(Response, result.Value);

        return NoContent();
    }
}
