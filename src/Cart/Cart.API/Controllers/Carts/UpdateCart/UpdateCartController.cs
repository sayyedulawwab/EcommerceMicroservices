using Cart.API.Extensions;
using Cart.Application.Carts.UpdateCart;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cart.API.Controllers.Carts.UpdateCart;
[Route("api/carts")]
[ApiController]
public class UpdateCartController : ControllerBase
{
    private readonly ISender _sender;
    public UpdateCartController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPut]
    public async Task<IActionResult> UpdateCart([FromBody] UpdateCartRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
        var userId = long.Parse(userIdClaim.Value);

        var cartItems = request.cartItems.Select(item =>
            new CartItemCommand(item.productId, item.productName, item.priceAmount, item.priceCurrency, item.quantity))
            .ToList();

        var command = new UpdateCartCommand(userId, cartItems);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }
        return Created(string.Empty, result.Value);
    }

}