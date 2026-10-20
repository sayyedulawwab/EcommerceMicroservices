using SharedKernel.Messaging;

namespace Cart.Application.Carts.ReplaceCart;

public record CartItemCommand(long ProductId, string ProductName, decimal PriceAmount, string PriceCurrency, int Quantity);

public record ReplaceCartCommand(long UserId, List<CartItemCommand> CartItems) : ICommand<CartResponse>;
