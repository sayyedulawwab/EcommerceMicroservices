using SharedKernel.Messaging;

namespace Ordering.Application.Orders.DeliverOrder;

public record DeliverOrderCommand(long OrderId) : ICommand;
