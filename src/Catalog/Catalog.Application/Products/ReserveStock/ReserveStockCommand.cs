using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.ReserveStock;

public record ReserveStockCommand(Guid OrderId, List<OrderStockItem> Items) : ICommand<ReserveStockResult>;
