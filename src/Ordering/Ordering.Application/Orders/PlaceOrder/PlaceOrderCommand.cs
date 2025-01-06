using Ordering.Application.Abstractions.Messaging;

namespace Ordering.Application.Orders.PlaceOrder;

public record OrderItemCommand(long ProductId, string ProductName, decimal PriceAmount, string PriceCurrency, int Quantity);
public record PlaceOrderCommand(long UserId, List<OrderItemCommand> OrderItems) : ICommand<long>;