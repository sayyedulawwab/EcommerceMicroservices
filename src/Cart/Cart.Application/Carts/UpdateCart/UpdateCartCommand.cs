using SharedKernel.Messaging;

namespace Cart.Application.Carts.UpdateCart;

public record UpdateCartCommand(long UserId, List<CartItemCommand> CartItems) : ICommand<Guid>;