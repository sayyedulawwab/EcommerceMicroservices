namespace Cart.Application.Carts.Commands.RemoveCart;
public record RemoveCartCommand(long userId) : Abstractions.Messaging.ICommand;
