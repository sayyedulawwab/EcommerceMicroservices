using Asp.Versioning;
using Cart.API.Extensions;
using Cart.Application.Carts;
using Cart.Application.Carts.AddCartItem;
using Cart.Application.Carts.GetCartByUser;
using Cart.Application.Carts.RemoveCart;
using Cart.Application.Carts.RemoveCartItem;
using Cart.Application.Carts.ReplaceCart;
using Cart.Application.Carts.UpdateCartItem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Cart.API.Controllers.Carts;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/carts")]
[ApiController]
[Authorize]
public class CartsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCart(IQueryHandler<GetCartByUserQuery, CartResponse> handler, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not long userId)
        {
            return Unauthorized();
        }

        Result<CartResponse> result = await handler.Handle(new GetCartByUserQuery(userId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>Replaces the whole cart with the given items. An empty list clears it.</summary>
    [HttpPut]
    public async Task<IActionResult> ReplaceCart([FromBody] UpdateCartRequest request, ICommandHandler<ReplaceCartCommand, CartResponse> handler, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not long userId)
        {
            return Unauthorized();
        }

        var cartItems = (request.CartItems ?? []).Select(item =>
            new CartItemCommand(item.ProductId, item.ProductName, item.PriceAmount, item.PriceCurrency, item.Quantity))
            .ToList();

        Result<CartResponse> result = await handler.Handle(new ReplaceCartCommand(userId, cartItems), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart(ICommandHandler<RemoveCartCommand> handler, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not long userId)
        {
            return Unauthorized();
        }

        Result result = await handler.Handle(new RemoveCartCommand(userId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return NoContent();
    }

    /// <summary>Adds a product to the cart, or increases its quantity if it is already there.</summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] CartItemRequest request, ICommandHandler<AddCartItemCommand, CartResponse> handler, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not long userId)
        {
            return Unauthorized();
        }

        var command = new AddCartItemCommand(
            userId,
            request.ProductId,
            request.ProductName,
            request.PriceAmount,
            request.PriceCurrency,
            request.Quantity);

        Result<CartResponse> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>Sets the quantity of a product in the cart; zero removes it.</summary>
    [HttpPut("items/{productId:long}")]
    public async Task<IActionResult> UpdateItemQuantity(long productId, [FromBody] UpdateCartItemQuantityRequest request, ICommandHandler<UpdateCartItemQuantityCommand, CartResponse> handler, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not long userId)
        {
            return Unauthorized();
        }

        Result<CartResponse> result = await handler.Handle(new UpdateCartItemQuantityCommand(userId, productId, request.Quantity), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpDelete("items/{productId:long}")]
    public async Task<IActionResult> RemoveItem(long productId, ICommandHandler<RemoveCartItemCommand, CartResponse> handler, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not long userId)
        {
            return Unauthorized();
        }

        Result<CartResponse> result = await handler.Handle(new RemoveCartItemCommand(userId, productId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
