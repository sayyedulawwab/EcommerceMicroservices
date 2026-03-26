using SharedKernel.Messaging;

namespace Ordering.Application.Orders.UpdateOrderStatus;

public record UpdateOrderStatusToPaidCommand(long OrderId) : ICommand<long>;