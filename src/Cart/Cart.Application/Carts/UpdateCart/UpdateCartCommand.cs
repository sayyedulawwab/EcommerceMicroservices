using Cart.Application.Abstractions.Messaging;

namespace Cart.Application.Carts.UpdateCart;

public record CartItemCommand(long productId, string productName, decimal priceAmount, string priceCurrency, int quantity);
public record UpdateCartCommand(long userId, List<CartItemCommand> cartItems) : ICommand<Guid>;
