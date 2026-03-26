using SharedKernel.Messaging;

namespace Catalog.Application.Products.DeleteProduct;
public record DeleteProductCommand(long Id) : ICommand<long>;