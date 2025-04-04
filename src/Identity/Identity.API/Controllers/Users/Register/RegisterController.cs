using Asp.Versioning;
using Identity.API.Extensions;
using Identity.Application.Users.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Identity.API.Controllers.Users.Register;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/auth/register")]
[ApiController]
public class RegisterControllerr(ISender sender) : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpPost]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password);

        Result<long> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
