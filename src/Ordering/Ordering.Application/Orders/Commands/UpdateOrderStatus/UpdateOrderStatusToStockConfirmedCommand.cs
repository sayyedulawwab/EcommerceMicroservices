using Ordering.Application.Abstractions.Messaging;
using Ordering.Application.Orders.Events;

namespace Ordering.Application.Orders.Commands.UpdateOrderStatus;

public record UpdateOrderStatusToPaidCommand(long orderId) : ICommand<long>;
