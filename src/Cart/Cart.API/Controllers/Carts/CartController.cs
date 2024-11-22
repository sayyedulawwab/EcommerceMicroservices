using Cart.Application.Carts.Commands;
using Cart.Application.Carts.Commands.UpdateCart;
using Cart.Application.Carts.Queries.GetCartByUserId;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cart.API.Controllers.Carts;
[Route("api/cart")]
[ApiController]
public class CartController : ControllerBase
{
    private readonly ISender _sender;
    public CartController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
        var userId = Int64.Parse(userIdClaim.Value);

        var query = new GetCartByUserIdQuery(userId);

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateCart([FromBody] UpdateCartRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
        var userId = Int64.Parse(userIdClaim.Value);

        var cartItems = request.cartItems.Select(item =>
            new CartItemCommand(item.productId, item.productName, item.priceAmount, item.priceCurrency, item.quantity))
            .ToList();

        var command = new UpdateCartCommand(userId, cartItems);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return Created(string.Empty, result.Value);
    }

}