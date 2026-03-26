using SharedKernel.Messaging;

namespace Cart.Application.Carts.GetCartByUser;

public record GetCartByUserQuery(long UserId) : IQuery<CartResponse>;
