namespace Ordering.API.Controllers.Orders.PlaceOrder;

public record OrderItemRequest(long ProductId, string ProductName, decimal PriceAmount, string PriceCurrency, int Quantity);
public record PlaceOrderRequest(List<OrderItemRequest> OrderItems);
