using Cart.Application.Carts.GetCartByUserId;
using Cart.Application.Carts.UpdateCart;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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
        //var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
        //var userId = Int64.Parse(userIdClaim.Value);
        var userId = 123;
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
        //var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
        //var userId = Int64.Parse(userIdClaim.Value);
        var userId = 123;

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
