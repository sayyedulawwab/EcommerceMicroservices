namespace Cart.Application.Carts.UpdateCart;
public record CartItemCommand(long productId, string productName, decimal priceAmount, string priceCurrency, int quantity);