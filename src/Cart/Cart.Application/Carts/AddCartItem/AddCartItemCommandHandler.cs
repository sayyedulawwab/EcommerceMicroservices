using Cart.Domain.Carts;
using SharedKernel.Domain;
using SharedKernel.Exceptions;
using SharedKernel.Messaging;

namespace Cart.Application.Carts.AddCartItem;

internal sealed class AddCartItemCommandHandler(
    ICartRepository cartRepository,
    TimeProvider timeProvider)
    : ICommandHandler<AddCartItemCommand, CartResponse>
{
    public async Task<Result<CartResponse>> HandleAsync(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        Result<Currency> currency = Currency.FromCode(request.PriceCurrency);

        if (currency.IsFailure)
        {
            return Result.Failure<CartResponse>(ValidationError.Collect(currency));
        }

        Domain.Carts.Cart cart = await cartRepository.GetByUserId(request.UserId, cancellationToken)
            ?? Domain.Carts.Cart.Create(request.UserId, timeProvider.GetUtcNow().UtcDateTime);

        Result addResult = cart.AddItem(
            request.ProductId,
            request.ProductName,
            new Money(request.PriceAmount, currency.Value),
            request.Quantity,
            timeProvider.GetUtcNow().UtcDateTime);

        if (addResult.IsFailure)
        {
            return Result.Failure<CartResponse>(addResult.Error);
        }

        await cartRepository.SaveAsync(cart, cancellationToken);

        return CartResponse.FromCart(cart);
    }
}
