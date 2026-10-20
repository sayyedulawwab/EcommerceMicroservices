using Cart.Application.Abstractions.Clock;
using Cart.Domain.Carts;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Cart.Application.Carts.AddCartItem;

internal sealed class AddCartItemCommandHandler(
    ICartRepository cartRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddCartItemCommand, CartResponse>
{
    public async Task<Result<CartResponse>> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        Domain.Carts.Cart cart = await cartRepository.GetByUserId(request.UserId, cancellationToken)
            ?? Domain.Carts.Cart.Create(request.UserId, dateTimeProvider.UtcNow);

        Result addResult = cart.AddItem(
            request.ProductId,
            request.ProductName,
            new Money(request.PriceAmount, Currency.FromCode(request.PriceCurrency)),
            request.Quantity,
            dateTimeProvider.UtcNow);

        if (addResult.IsFailure)
        {
            return Result.Failure<CartResponse>(addResult.Error);
        }

        await cartRepository.SaveAsync(cart, cancellationToken);

        return CartResponse.FromCart(cart);
    }
}
