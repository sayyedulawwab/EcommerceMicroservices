namespace Cart.Application.Carts.Commands.RemoveCart;
public record RemoveCartCommand(long userId, List<CartItemCommand> cartItems) : Abstractions.Messaging.ICommand;
