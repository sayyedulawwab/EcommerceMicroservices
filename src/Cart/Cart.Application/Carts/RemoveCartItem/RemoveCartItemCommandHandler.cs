using Cart.Application.Abstractions.Clock;
using Cart.Domain.Carts;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Cart.Application.Carts.RemoveCartItem;

internal sealed class RemoveCartItemCommandHandler(
    ICartRepository cartRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RemoveCartItemCommand, CartResponse>
{
    public async Task<Result<CartResponse>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        Domain.Carts.Cart? cart = await cartRepository.GetByUserId(request.UserId, cancellationToken);

        if (cart is null)
        {
            return Result.Failure<CartResponse>(CartErrors.ItemNotFound(request.ProductId));
        }

        Result result = cart.RemoveItem(request.ProductId, dateTimeProvider.UtcNow);

        if (result.IsFailure)
        {
            return Result.Failure<CartResponse>(result.Error);
        }

        await cartRepository.SaveAsync(cart, cancellationToken);

        return CartResponse.FromCart(cart);
    }
}
