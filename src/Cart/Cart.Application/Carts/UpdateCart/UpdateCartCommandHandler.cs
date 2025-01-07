using Cart.Application.Abstractions.Clock;
using Cart.Application.Abstractions.Messaging;
using Cart.Domain.Carts;
using SharedKernel.Domain;

namespace Cart.Application.Carts.UpdateCart;
internal sealed class UpdateCartCommandHandler(
    ICartRepository cartRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateCartCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UpdateCartCommand request, CancellationToken cancellationToken)
    {
        var cart = Domain.Carts.Cart.Create(request.UserId, dateTimeProvider.UtcNow);

        foreach (CartItemCommand item in request.CartItems)
        {
            cart.AddCartItem(cart.Id,
                item.ProductId,
                item.ProductName,
                new Money(item.PriceAmount, Currency.Create(item.PriceCurrency)),
                item.Quantity,
                dateTimeProvider.UtcNow);
        }

        await cartRepository.RemoveAsync(request.UserId);

        await cartRepository.AddAsync(cart);

        return cart.Id;
    }
}