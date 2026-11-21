using Cart.Domain.Carts;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Cart.Application.Carts.UpdateCartItem;

internal sealed class UpdateCartItemQuantityCommandHandler(
    ICartRepository cartRepository,
    TimeProvider timeProvider)
    : ICommandHandler<UpdateCartItemQuantityCommand, CartResponse>
{
    public async Task<Result<CartResponse>> HandleAsync(UpdateCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        Domain.Carts.Cart? cart = await cartRepository.GetByUserId(request.UserId, cancellationToken);

        if (cart is null)
        {
            return Result.Failure<CartResponse>(CartErrors.ItemNotFound(request.ProductId));
        }

        Result result = cart.SetItemQuantity(request.ProductId, request.Quantity, timeProvider.GetUtcNow().UtcDateTime);

        if (result.IsFailure)
        {
            return Result.Failure<CartResponse>(result.Error);
        }

        await cartRepository.SaveAsync(cart, cancellationToken);

        return CartResponse.FromCart(cart);
    }
}
