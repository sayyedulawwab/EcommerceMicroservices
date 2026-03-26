using Asp.Versioning;
using Cart.API.Extensions;
using Cart.Application.Carts.GetCartByUser;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;
using System.Globalization;
using System.Security.Claims;

namespace Cart.API.Controllers.Carts.GetCartByUser;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/carts")]
[ApiController]
public class GetCartByUserController() : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpGet]
    public async Task<IActionResult> GetCart(IQueryHandler<GetCartByUserQuery, CartResponse> handler, CancellationToken cancellationToken)
    {
        Claim? userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null)
        {
            return BadRequest("User not found");
        }

        long userId = long.Parse(userIdClaim.Value, CultureInfo.InvariantCulture);

        var query = new GetCartByUserQuery(userId);

        Result<CartResponse> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
