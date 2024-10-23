using Cart.Application.Abstractions.Messaging;

namespace Cart.Application.Carts.Queries.GetCartByUserId;
public record GetCartByUserIdQuery(long userId) : IQuery<CartResponse>;
