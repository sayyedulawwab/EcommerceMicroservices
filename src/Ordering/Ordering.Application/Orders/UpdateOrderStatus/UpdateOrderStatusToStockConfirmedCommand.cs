using Ordering.Application.Abstractions.Messaging;

namespace Ordering.Application.Orders.UpdateOrderStatus;

public record UpdateOrderStatusToPaidCommand(long orderId) : ICommand<long>;