using Cart.Application.Abstractions.Messaging;

namespace Cart.Application.Carts.Commands.UpdateCart;

public record UpdateCartCommand(long userId, List<CartItemCommand> cartItems) : ICommand<Guid>;
