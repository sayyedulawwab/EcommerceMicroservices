using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.DeleteProduct;

internal sealed class DeleteProductCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<DeleteProductCommand, long>
{
    public async Task<Result<long>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            return Result.Failure<long>(ProductErrors.NotFound());
        }

        dbContext.Products.Remove(product);

        await dbContext.SaveChangesAsync(cancellationToken);

        return product.Id;

    }
}