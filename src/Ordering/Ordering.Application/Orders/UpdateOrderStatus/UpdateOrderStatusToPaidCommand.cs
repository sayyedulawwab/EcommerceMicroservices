using Ordering.Application.Abstractions.Messaging;

namespace Ordering.Application.Orders.UpdateOrderStatus;

public record UpdateOrderStatusToStockConfirmedCommand(long OrderId) : ICommand<long>;