using Cart.Domain.Carts;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Cart.Application.Carts.GetCartByUser;

internal sealed class GetCartByUserQueryHandler(ICartRepository cartRepository) : IQueryHandler<GetCartByUserQuery, CartResponse>
{
    public async Task<Result<CartResponse>> Handle(GetCartByUserQuery request, CancellationToken cancellationToken)
    {
        Domain.Carts.Cart? cart = await cartRepository.GetByUserId(request.UserId, cancellationToken);

        return cart is null
            ? CartResponse.Empty(request.UserId)
            : CartResponse.FromCart(cart);
    }
}
