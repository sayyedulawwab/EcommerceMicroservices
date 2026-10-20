using Asp.Versioning;
using Identity.API.Auth;
using Identity.API.Extensions;
using Identity.Application.Users;
using Identity.Application.Users.ConfirmEmail;
using Identity.Application.Users.ForgotPassword;
using Identity.Application.Users.Login;
using Identity.Application.Users.Logout;
using Identity.Application.Users.RefreshTokens;
using Identity.Application.Users.Register;
using Identity.Application.Users.ResendEmailConfirmation;
using Identity.Application.Users.ResetPassword;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Auth;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.API.Controllers.Auth;

/// <summary>
/// Sign-up, sign-in and account recovery. Tokens are only ever set as HttpOnly cookies, never returned in a body.
/// </summary>
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/auth")]
[ApiController]
[AllowAnonymous]
public class AuthController(AuthCookieWriter authCookieWriter) : ControllerBase
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

        return Created(string.Empty, new { id = result.Value });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        ICommandHandler<LoginCommand, AuthTokens> handler,
        CancellationToken cancellationToken)
    {
        Result<AuthTokens> result = await handler.Handle(new LoginCommand(request.Email, request.Password), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        authCookieWriter.Write(Response, result.Value);

        return NoContent();
    }

    /// <summary>Rotates the refresh token cookie and issues a new access token cookie.</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        ICommandHandler<RefreshTokensCommand, AuthTokens> handler,
        CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(AuthCookies.RefreshToken, out string? refreshToken);

        Result<AuthTokens> result = await handler.Handle(new RefreshTokensCommand(refreshToken), cancellationToken);

        if (result.IsFailure)
        {
            authCookieWriter.Clear(Response);

            return result.Error.ToActionResult();
        }

        authCookieWriter.Write(Response, result.Value);

        return NoContent();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        ICommandHandler<LogoutCommand> handler,
        CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(AuthCookies.RefreshToken, out string? refreshToken);

        Result result = await handler.Handle(new LogoutCommand(refreshToken), cancellationToken);

        authCookieWriter.Clear(Response);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return NoContent();
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        ConfirmEmailRequest request,
        ICommandHandler<ConfirmEmailCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.Handle(new ConfirmEmailCommand(request.Email, request.Token), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return NoContent();
    }

    /// <summary>Mails a new confirmation link. Answers 202 whether or not the account exists.</summary>
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation(
        EmailRequest request,
        ICommandHandler<ResendEmailConfirmationCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.Handle(new ResendEmailConfirmationCommand(request.Email), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Accepted();
    }

    /// <summary>Mails a password reset link. Answers 202 whether or not the account exists.</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        EmailRequest request,
        ICommandHandler<ForgotPasswordCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.Handle(new ForgotPasswordCommand(request.Email), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Accepted();
    }

    /// <summary>Sets a new password with the token from the reset link and signs out every session.</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        ICommandHandler<ResetPasswordCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.Handle(
            new ResetPasswordCommand(request.Email, request.Token, request.NewPassword),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        authCookieWriter.Clear(Response);

        return NoContent();
    }
}
