using Cart.API.Extensions;
using Cart.Application.Carts.UpdateCart;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using System.Globalization;
using System.Security.Claims;

namespace Cart.API.Controllers.Carts.UpdateCart;
[Route("api/carts")]
[ApiController]
public class UpdateCartController(ISender sender) : ControllerBase
{
    [HttpPut]
    public async Task<IActionResult> UpdateCart([FromBody] UpdateCartRequest request, CancellationToken cancellationToken)
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

        Result<Guid> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }
        return Created(string.Empty, result.Value);
    }

}