using SharedKernel.Messaging;

namespace Cart.Application.Carts.RemoveCartItem;

public record RemoveCartItemCommand(Guid UserId, Guid ProductId) : ICommand<CartResponse>;
