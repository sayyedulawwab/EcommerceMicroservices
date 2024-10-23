using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Products.Commands.DeleteProduct;
public record DeleteProductCommand(long id) : ICommand<long>;
