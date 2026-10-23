using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.EditProduct;

internal sealed class EditProductCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<EditProductCommand, long>
{
    public async Task<Result<long>> Handle(EditProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            return Result.Failure<long>(ProductErrors.NotFound());
        }

        if (product.CategoryId != request.CategoryId &&
            !await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            return Result.Failure<long>(ProductErrors.CategoryNotFound(request.CategoryId));
        }

        Product.Update(
            product,
            request.Name,
            request.Description,
            new Money(request.PriceAmount, Currency.FromCode(request.PriceCurrency)),
            request.Quantity,
            request.CategoryId,
            dateTimeProvider.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return product.Id;

    }
}
