using Cart.Application.Abstractions.Clock;
using Cart.Application.Abstractions.Messaging;
using Cart.Domain.Abstractions;
using Cart.Domain.Carts;
using Cart.Domain.Shared;

namespace Cart.Application.Carts.Commands.UpdateCart;
internal sealed class UpdateCartCommandHandler : ICommandHandler<UpdateCartCommand, Guid>
{
    private readonly ICartRepository _cartRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateCartCommandHandler(ICartRepository cartRepository, IDateTimeProvider dateTimeProvider)
    {
        _cartRepository = cartRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(UpdateCartCommand request, CancellationToken cancellationToken)
    {

        var newCartItems = new List<CartItem>();

        var cart = Domain.Carts.Cart.Create(request.userId, _dateTimeProvider.UtcNow);

        foreach (var item in request.cartItems)
        {
            cart.AddCartItem(cart.Id,
                item.productId,
                item.productName,
                new Money(item.priceAmount, Currency.Create(item.priceCurrency)),
                item.quantity,
                _dateTimeProvider.UtcNow);
        }

        await _cartRepository.RemoveAsync(cart);

        await _cartRepository.AddAsync(cart);

        return cart.Id;
    }
}
