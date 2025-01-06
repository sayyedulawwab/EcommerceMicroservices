namespace Cart.Application.Carts.UpdateCart;
public record CartItemCommand(long ProductId, string ProductName, decimal PriceAmount, string PriceCurrency, int Quantity);