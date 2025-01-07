using Identity.API.Extensions;
using Identity.Application.Users.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Identity.API.Controllers.Users.Login;
[Route("api/auth/login")]
[ApiController]
public class LoginController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var query = new LoginUserQuery(request.Email, request.Password);

        Result<AccessTokenResponse> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
