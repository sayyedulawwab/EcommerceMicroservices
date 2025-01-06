using Cart.API.Extensions;
using Cart.Application.Carts.GetCartByUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using System.Globalization;
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
        Claim? userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null)
        {
            return BadRequest("User not found");
        }

        long userId = long.Parse(userIdClaim.Value, CultureInfo.InvariantCulture);

        var query = new GetCartByUserQuery(userId);

        Result<CartResponse> result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
