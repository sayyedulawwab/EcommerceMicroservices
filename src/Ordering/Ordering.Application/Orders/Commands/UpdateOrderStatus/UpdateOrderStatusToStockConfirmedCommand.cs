using Ordering.Application.Abstractions.Messaging;

namespace Ordering.Application.Orders.Commands.UpdateOrderStatus;

public record UpdateOrderStatusToPaidCommand(long orderId) : ICommand<long>;