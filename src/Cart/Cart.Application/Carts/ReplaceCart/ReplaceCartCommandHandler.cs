using Cart.Domain.Carts;
using SharedKernel.Domain;
using SharedKernel.Exceptions;
using SharedKernel.Messaging;

namespace Cart.Application.Carts.ReplaceCart;

internal sealed class ReplaceCartCommandHandler(
    ICartRepository cartRepository,
    TimeProvider timeProvider)
    : ICommandHandler<ReplaceCartCommand, CartResponse>
{
    public async Task<Result<CartResponse>> HandleAsync(ReplaceCartCommand request, CancellationToken cancellationToken)
    {
        if (request.CartItems.Count == 0)
        {
            await cartRepository.RemoveAsync(request.UserId, cancellationToken);

            return CartResponse.Empty(request.UserId);
        }

        var cart = Domain.Carts.Cart.Create(request.UserId, timeProvider.GetUtcNow().UtcDateTime);

        foreach (CartItemCommand item in request.CartItems)
        {
            Result<Currency> currency = Currency.FromCode(item.PriceCurrency);

            if (currency.IsFailure)
            {
                return Result.Failure<CartResponse>(ValidationError.Collect(currency));
            }

            Result addResult = cart.AddItem(
                item.ProductId,
                item.ProductName,
                new Money(item.PriceAmount, currency.Value),
                item.Quantity,
                timeProvider.GetUtcNow().UtcDateTime);

            if (addResult.IsFailure)
            {
                return Result.Failure<CartResponse>(addResult.Error);
            }
        }

        await cartRepository.SaveAsync(cart, cancellationToken);

        return CartResponse.FromCart(cart);
    }
}
