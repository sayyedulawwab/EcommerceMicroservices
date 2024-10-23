namespace Cart.Application.Carts.Commands;

public record CartItemCommand(long productId, string productName, decimal priceAmount, string priceCurrency, int quantity);
