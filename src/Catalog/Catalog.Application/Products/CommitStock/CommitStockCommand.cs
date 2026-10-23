using SharedKernel.Messaging;

namespace Catalog.Application.Products.CommitStock;

public record CommitStockCommand(Guid OrderId) : ICommand;
