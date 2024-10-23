using Cart.Application.Abstractions.Messaging;

namespace Cart.Application.Carts.Commands.RemoveCart;
public record RemoveCartCommand(long userId, List<CartItemCommand> cartItems) : ICommand<Guid>;
