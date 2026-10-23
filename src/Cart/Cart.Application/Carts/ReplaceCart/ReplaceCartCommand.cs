using SharedKernel.Messaging;

namespace Cart.Application.Carts.ReplaceCart;

public record CartItemCommand(Guid ProductId, string ProductName, decimal PriceAmount, string PriceCurrency, int Quantity);

public record ReplaceCartCommand(Guid UserId, List<CartItemCommand> CartItems) : ICommand<CartResponse>;
