using Cart.API.Extensions;
using Cart.Application.Carts.GetCartByUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cart.API.Controllers.Carts.GetCartByUser;
[Route("api/carts")]
[ApiController]
public class GetCartByUserController : ControllerBase
{
    private readonly ISender _sender;
    public GetCartByUserController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
        var userId = long.Parse(userIdClaim.Value);

        var query = new GetCartByUserQuery(userId);

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
