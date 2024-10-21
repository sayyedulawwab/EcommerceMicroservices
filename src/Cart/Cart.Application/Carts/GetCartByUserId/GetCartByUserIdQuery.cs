using Cart.Application.Abstractions.Messaging;

namespace Cart.Application.Carts.GetCartByUserId;
public record GetCartByUserIdQuery(long userId) : IQuery<CartResponse>;
