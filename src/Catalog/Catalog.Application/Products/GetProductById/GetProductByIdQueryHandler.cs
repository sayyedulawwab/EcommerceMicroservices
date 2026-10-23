using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.GetProductById;

internal sealed class GetProductByIdQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        Product? product = await dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductResponse>(ProductErrors.NotFound());
        }

        var productResponse = new ProductResponse()
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            PriceAmount = product.Price.Amount,
            PriceCurrency = product.Price.Currency.Code,
            Quantity = product.Quantity,
            CategoryId = product.CategoryId,
            CreatedOnUtc = product.CreatedOnUtc,
            UpdatedOnUtc = product.UpdatedOnUtc,
        };

        return productResponse;
    }
}