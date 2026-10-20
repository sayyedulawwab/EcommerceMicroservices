using SharedKernel.Messaging;

namespace Catalog.Application.Products.ReleaseStock;

public record ReleaseStockCommand(long OrderId) : ICommand;
