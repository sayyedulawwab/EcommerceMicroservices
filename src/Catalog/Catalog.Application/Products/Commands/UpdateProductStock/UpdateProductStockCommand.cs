using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Products.Commands.EditProduct;
public record UpdateProductStockCommand(long id, int quantity) : ICommand<long>;
