using Ordering.Application.Abstractions.Messaging;

namespace Ordering.Application.Orders.Commands.UpdateOrderStatus;

public record UpdateOrderStatusToStockConfirmedCommand(long orderId) : ICommand<long>;