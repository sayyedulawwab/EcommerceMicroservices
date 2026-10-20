using SharedKernel.Messaging;

namespace Cart.Application.Carts.RemoveCartItem;

public record RemoveCartItemCommand(long UserId, long ProductId) : ICommand<CartResponse>;
