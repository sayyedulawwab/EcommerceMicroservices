using Cart.Application.Abstractions.Clock;
using Cart.Application.Abstractions.Messaging;
using Cart.Domain.Abstractions;
using Cart.Domain.Carts;
using Cart.Domain.Shared;

namespace Cart.Application.Carts.Commands.RemoveCart;
internal sealed class RemoveCartCommandHandler : ICommandHandler<RemoveCartCommand, Guid>
{
    private readonly ICartRepository _cartRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemoveCartCommandHandler(ICartRepository cartRepository, IDateTimeProvider dateTimeProvider)
    {
        _cartRepository = cartRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(RemoveCartCommand request, CancellationToken cancellationToken)
    {

        var newCartItems = new List<CartItem>();

        var cart = Domain.Carts.Cart.Create(request.userId, newCartItems, _dateTimeProvider.UtcNow);

        newCartItems = request.cartItems
          .Select(item => CartItem.Create(
              cart.Id,
              item.productId,
              item.productName,
              new Money(item.priceAmount, Currency.Create(item.priceCurrency)),
              item.quantity,
              _dateTimeProvider.UtcNow
          ))
          .ToList();

        cart.AddCartItems(newCartItems);

        await _cartRepository.RemoveAsync(cart);

        await _cartRepository.AddAsync(cart);

        return cart.Id;
    }
}
