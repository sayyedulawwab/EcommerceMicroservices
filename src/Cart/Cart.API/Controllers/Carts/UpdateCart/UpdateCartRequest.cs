namespace Cart.API.Controllers.Carts.UpdateCart;

public record CartItemRequest(long productId, string productName, decimal priceAmount, string priceCurrency, int quantity);
public record UpdateCartRequest(long userId, List<CartItemRequest> cartItems);