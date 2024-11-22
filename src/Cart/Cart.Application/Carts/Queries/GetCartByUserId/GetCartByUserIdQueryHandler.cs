using Cart.Application.Abstractions.Messaging;
using Cart.Domain.Carts;
using SharedKernel.Domain;

namespace Cart.Application.Carts.Queries.GetCartByUserId;
internal sealed class GetCartByUserIdQueryHandler : IQueryHandler<GetCartByUserIdQuery, CartResponse>
{
    private readonly ICartRepository _cartRepository;
    public GetCartByUserIdQueryHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }
    public async Task<Result<CartResponse>> Handle(GetCartByUserIdQuery request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByUserId(request.userId);

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
                CreatedOn = ci.CreatedOn,
                UpdatedOn = ci.UpdatedOn
            }).ToList(),
            TotalPriceAmount = cart.TotalPrice.Amount,
            TotalPriceCurrency = cart.TotalPrice.Currency.Code,
            CreatedOnUtc = cart.CreatedOnUtc,
            UpdatedOnUtc = cart.UpdatedOnUtc
        };

        return cartResponse;
    }
}
