namespace Ordering.API.Controllers.Orders;

public record OrderItemRequest(long productId, string productName, decimal priceAmount, string priceCurrency, int quantity);
public record PlaceOrderRequest(List<OrderItemRequest> orderItems);
