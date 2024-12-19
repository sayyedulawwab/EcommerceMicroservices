using Identity.API.Extensions;
using Identity.Application.Users.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers.Users.Login;
[Route("api/auth/login")]
[ApiController]
public class LoginController : ControllerBase
{
    private readonly ISender _sender;

    public LoginController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var query = new LoginUserQuery(request.email, request.password);

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
