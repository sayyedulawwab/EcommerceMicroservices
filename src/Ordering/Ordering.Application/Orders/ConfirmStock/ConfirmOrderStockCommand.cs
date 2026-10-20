using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.ConfirmStock;

public record ConfirmOrderStockCommand(long OrderId, List<PricedOrderItem> OrderItems) : ICommand;
