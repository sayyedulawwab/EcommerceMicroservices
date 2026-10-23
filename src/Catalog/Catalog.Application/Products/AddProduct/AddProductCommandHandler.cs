using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.AddProduct;

internal sealed class AddProductCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.CategoryId);

        if (!await dbContext.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
        {
            return Result.Failure<Guid>(ProductErrors.CategoryNotFound(request.CategoryId));
        }

        var product = Product.Create(
            request.Name,
            request.Description,
            new Money(request.PriceAmount, Currency.FromCode(request.PriceCurrency)),
            request.Quantity,
            categoryId,
            dateTimeProvider.UtcNow);

        dbContext.Products.Add(product);

        await dbContext.SaveChangesAsync(cancellationToken);

        return product.Id.Value;

    }
}
