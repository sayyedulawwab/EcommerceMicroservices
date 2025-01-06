using Ordering.Application.Abstractions.Messaging;

namespace Ordering.Application.Orders.UpdateOrderStatus;

public record UpdateOrderStatusToPaidCommand(long OrderId) : ICommand<long>;