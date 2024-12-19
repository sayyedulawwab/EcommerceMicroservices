namespace Cart.Application.Carts.RemoveCart;
public record RemoveCartCommand(long userId) : Abstractions.Messaging.ICommand;
