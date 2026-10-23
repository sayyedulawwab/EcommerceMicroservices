using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.ConfirmStock;

public record ConfirmOrderStockCommand(Guid OrderId, List<PricedOrderItem> OrderItems) : ICommand;
