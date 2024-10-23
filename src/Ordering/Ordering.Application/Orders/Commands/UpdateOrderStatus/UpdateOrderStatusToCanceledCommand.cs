using Ordering.Application.Abstractions.Messaging;

namespace Ordering.Application.Orders.Commands.UpdateOrderStatus;

public record UpdateOrderStatusToCanceledCommand(long orderId) : ICommand<long>;
