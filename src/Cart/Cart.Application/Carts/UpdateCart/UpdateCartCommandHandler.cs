using Cart.Application.Abstractions.Clock;
using Cart.Application.Abstractions.Messaging;
using Cart.Domain.Carts;
using SharedKernel.Domain;

namespace Cart.Application.Carts.UpdateCart;
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
        var cart = Domain.Carts.Cart.Create(request.UserId, _dateTimeProvider.UtcNow);

        foreach (CartItemCommand item in request.CartItems)
        {
            cart.AddCartItem(cart.Id,
                item.ProductId,
                item.ProductName,
                new Money(item.PriceAmount, Currency.Create(item.PriceCurrency)),
                item.Quantity,
                _dateTimeProvider.UtcNow);
        }

        await _cartRepository.RemoveAsync(request.UserId);

        await _cartRepository.AddAsync(cart);

        return cart.Id;
    }
}