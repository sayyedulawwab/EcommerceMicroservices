using Ordering.Application.Abstractions.Messaging;

namespace Ordering.Application.Orders.UpdateOrderStatus;

public record UpdateOrderStatusToCanceledCommand(long orderId) : ICommand<long>;