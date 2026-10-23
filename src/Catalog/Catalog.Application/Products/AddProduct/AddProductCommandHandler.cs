using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.AddProduct;

internal sealed class AddProductCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddProductCommand, long>
{
    public async Task<Result<long>> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            return Result.Failure<long>(ProductErrors.CategoryNotFound(request.CategoryId));
        }

        var product = Product.Create(
            request.Name,
            request.Description,
            new Money(request.PriceAmount, Currency.FromCode(request.PriceCurrency)),
            request.Quantity,
            request.CategoryId,
            dateTimeProvider.UtcNow);

        dbContext.Products.Add(product);

        await dbContext.SaveChangesAsync(cancellationToken);

        return product.Id;

    }
}
