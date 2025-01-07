using Cart.Application.Abstractions.Messaging;
using Cart.Domain.Carts;
using SharedKernel.Domain;

namespace Cart.Application.Carts.GetCartByUser;
internal sealed class GetCartByUserQueryHandler(ICartRepository cartRepository) : IQueryHandler<GetCartByUserQuery, CartResponse>
{
    public async Task<Result<CartResponse>> Handle(GetCartByUserQuery request, CancellationToken cancellationToken)
    {
        Domain.Carts.Cart? cart = await cartRepository.GetByUserId(request.UserId, cancellationToken);

        if (cart is null)
        {
            return Result.Failure<CartResponse>(CartErrors.NotFound());
        }

        var cartResponse = new CartResponse
        {
            Id = cart.Id,
            UserId = cart.UserId,
            CartItems = cart.CartItems.Select(ci => new CartItemResponse
            {
                ProductId = ci.ProductId,
                ProductName = ci.ProductName,
                ProductPriceAmount = ci.Price.Amount,
                ProductPriceCurrency = ci.Price.Currency.Code,
                Quantity = ci.Quantity,
                CreatedOnUtc = ci.CreatedOnUtc
            }).ToList(),
            TotalPriceAmount = cart.TotalPrice.Amount,
            TotalPriceCurrency = cart.TotalPrice.Currency.Code,
            CreatedOnUtc = cart.CreatedOnUtc
        };

        return cartResponse;
    }
}
