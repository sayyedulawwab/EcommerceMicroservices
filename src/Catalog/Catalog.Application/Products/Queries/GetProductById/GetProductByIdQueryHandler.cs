using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Products;
using SharedKernel.Domain;

namespace Catalog.Application.Products.Queries.GetProductById;
internal sealed class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.id, cancellationToken);

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