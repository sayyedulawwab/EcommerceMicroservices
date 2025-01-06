using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Products.DeleteProduct;
public record DeleteProductCommand(long Id) : ICommand<long>;