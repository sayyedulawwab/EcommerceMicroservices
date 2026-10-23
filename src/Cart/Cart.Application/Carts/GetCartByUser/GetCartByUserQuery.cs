using SharedKernel.Messaging;

namespace Cart.Application.Carts.GetCartByUser;

public record GetCartByUserQuery(Guid UserId) : IQuery<CartResponse>;
