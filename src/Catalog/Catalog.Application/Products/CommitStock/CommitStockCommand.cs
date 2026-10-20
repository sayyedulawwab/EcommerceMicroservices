using SharedKernel.Messaging;

namespace Catalog.Application.Products.CommitStock;

public record CommitStockCommand(long OrderId) : ICommand;
