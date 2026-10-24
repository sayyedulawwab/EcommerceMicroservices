using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Exceptions;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.EditProduct;

internal sealed class EditProductCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<EditProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(EditProductCommand request, CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.Id);
        var categoryId = new CategoryId(request.CategoryId);

        Product? product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
        {
            return Result.Failure<Guid>(ProductErrors.NotFound());
        }

        if (product.CategoryId != categoryId &&
            !await dbContext.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
        {
            return Result.Failure<Guid>(ProductErrors.CategoryNotFound(request.CategoryId));
        }

        Result<Currency> currency = Currency.FromCode(request.PriceCurrency);

        if (currency.IsFailure)
        {
            return Result.Failure<Guid>(ValidationError.FromResults([currency]));
        }

        Product.Update(
            product,
            request.Name,
            request.Description,
            new Money(request.PriceAmount, currency.Value),
            request.Quantity,
            categoryId,
            dateTimeProvider.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return product.Id.Value;

    }
}
