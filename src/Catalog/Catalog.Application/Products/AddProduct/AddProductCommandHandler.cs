using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Exceptions;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.AddProduct;

internal sealed class AddProductCommandHandler(
    IApplicationDbContext dbContext,
    TimeProvider timeProvider)
    : ICommandHandler<AddProductCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(AddProductCommand request, CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.CategoryId);

        if (!await dbContext.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
        {
            return Result.Failure<Guid>(ProductErrors.CategoryNotFound(request.CategoryId));
        }

        Result<Currency> currency = Currency.FromCode(request.PriceCurrency);

        if (currency.IsFailure)
        {
            return Result.Failure<Guid>(ValidationError.Collect(currency));
        }

        Result<Product> created = Product.Create(
            request.Name,
            request.Description,
            new Money(request.PriceAmount, currency.Value),
            request.Quantity,
            categoryId,
            timeProvider.GetUtcNow().UtcDateTime);

        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        Product product = created.Value;

        dbContext.Products.Add(product);

        await dbContext.SaveChangesAsync(cancellationToken);

        return product.Id.Value;

    }
}
