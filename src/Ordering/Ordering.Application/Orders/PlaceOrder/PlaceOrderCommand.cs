using Ordering.Application.Abstractions.Messaging;

namespace Ordering.Application.Orders.PlaceOrder;

public record OrderItemCommand(long productId, string productName, decimal priceAmount, string priceCurrency, int quantity);
public record PlaceOrderCommand(long userId, List<OrderItemCommand> orderItems) : ICommand<long>;
