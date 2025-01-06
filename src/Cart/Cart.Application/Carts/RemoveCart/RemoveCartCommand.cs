namespace Cart.Application.Carts.RemoveCart;
public record RemoveCartCommand(long UserId) : Abstractions.Messaging.ICommand;
