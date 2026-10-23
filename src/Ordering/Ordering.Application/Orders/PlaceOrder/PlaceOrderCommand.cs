using SharedKernel.Messaging;

namespace Ordering.Application.Orders.PlaceOrder;

public record OrderItemCommand(Guid ProductId, int Quantity);
public record PlaceOrderCommand(Guid UserId, List<OrderItemCommand> OrderItems) : ICommand<Guid>;
