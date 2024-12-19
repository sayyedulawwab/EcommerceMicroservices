using Identity.API.Extensions;
using Identity.Application.Users.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers.Users.Register;
[Route("api/auth/register")]
[ApiController]
public class RegisterController : ControllerBase
{
    private readonly ISender _sender;

    public RegisterController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.firstName,
            request.lastName,
            request.email,
            request.password);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
