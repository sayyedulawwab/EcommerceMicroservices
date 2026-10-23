using SharedKernel.Messaging;

namespace Catalog.Application.Products.ReleaseStock;

public record ReleaseStockCommand(Guid OrderId) : ICommand;
