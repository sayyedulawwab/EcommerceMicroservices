using SharedKernel.Messaging;

namespace Ordering.Application.Orders.DeliverOrder;

public record DeliverOrderCommand(Guid OrderId) : ICommand;
