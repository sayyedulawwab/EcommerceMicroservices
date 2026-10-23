using SharedKernel.Messaging;

namespace Cart.Application.Carts.UpdateCartItem;

/// <param name="Quantity">The new quantity; zero removes the item.</param>
public record UpdateCartItemQuantityCommand(Guid UserId, Guid ProductId, int Quantity) : ICommand<CartResponse>;
