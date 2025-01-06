using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Products.UpdateProductStock;
public record UpdateProductStockCommand(long Id, int Quantity) : ICommand<long>;