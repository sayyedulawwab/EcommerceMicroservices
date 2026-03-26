using Asp.Versioning;
using Cart.API.Extensions;
using Cart.Application.Carts.UpdateCart;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;
using System.Globalization;
using System.Security.Claims;

namespace Cart.API.Controllers.Carts.UpdateCart;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/carts")]
[ApiController]
public class UpdateCartController() : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpPut]
    public async Task<IActionResult> UpdateCart([FromBody] UpdateCartRequest request, ICommandHandler<UpdateCartCommand, Guid> handler, CancellationToken cancellationToken)
    {
        Claim? userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null)
        {
            return BadRequest("User not found");
        }

        long userId = long.Parse(userIdClaim.Value, CultureInfo.InvariantCulture);

        var cartItems = request.CartItems.Select(item =>
            new CartItemCommand(item.ProductId, item.ProductName, item.PriceAmount, item.PriceCurrency, item.Quantity))
            .ToList();

        var command = new UpdateCartCommand(userId, cartItems);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }
        return Created(string.Empty, result.Value);
    }

}