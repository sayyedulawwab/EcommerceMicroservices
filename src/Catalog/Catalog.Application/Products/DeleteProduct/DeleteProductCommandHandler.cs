using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.DeleteProduct;

internal sealed class DeleteProductCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<DeleteProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.Id);

        Product? product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
        {
            return Result.Failure<Guid>(ProductErrors.NotFound());
        }

        dbContext.Products.Remove(product);

        await dbContext.SaveChangesAsync(cancellationToken);

        return product.Id.Value;

    }
}