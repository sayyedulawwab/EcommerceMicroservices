using Cart.Application.Abstractions.Messaging;

namespace Cart.Application.Carts.GetCartByUser;
public record GetCartByUserQuery(long userId) : IQuery<CartResponse>;
