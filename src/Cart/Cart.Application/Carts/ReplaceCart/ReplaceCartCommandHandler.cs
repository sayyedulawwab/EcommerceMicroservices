using Cart.Application.Abstractions.Clock;
using Cart.Domain.Carts;
using SharedKernel.Domain;
using SharedKernel.Exceptions;
using SharedKernel.Messaging;

namespace Cart.Application.Carts.ReplaceCart;

internal sealed class ReplaceCartCommandHandler(
    ICartRepository cartRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ReplaceCartCommand, CartResponse>
{
    public async Task<Result<CartResponse>> Handle(ReplaceCartCommand request, CancellationToken cancellationToken)
    {
        if (request.CartItems.Count == 0)
        {
            await cartRepository.RemoveAsync(request.UserId, cancellationToken);

            return CartResponse.Empty(request.UserId);
        }

        var cart = Domain.Carts.Cart.Create(request.UserId, dateTimeProvider.UtcNow);

        foreach (CartItemCommand item in request.CartItems)
        {
            Result<Currency> currency = Currency.FromCode(item.PriceCurrency);

            if (currency.IsFailure)
            {
                return Result.Failure<CartResponse>(ValidationError.FromResults([currency]));
            }

            Result addResult = cart.AddItem(
                item.ProductId,
                item.ProductName,
                new Money(item.PriceAmount, currency.Value),
                item.Quantity,
                dateTimeProvider.UtcNow);

            if (addResult.IsFailure)
            {
                return Result.Failure<CartResponse>(addResult.Error);
            }
        }

        await cartRepository.SaveAsync(cart, cancellationToken);

        return CartResponse.FromCart(cart);
    }
}
