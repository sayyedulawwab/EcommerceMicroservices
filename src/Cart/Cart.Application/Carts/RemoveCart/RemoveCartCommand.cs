using SharedKernel.Messaging;

namespace Cart.Application.Carts.RemoveCart;

public record RemoveCartCommand(long UserId) : ICommand;
