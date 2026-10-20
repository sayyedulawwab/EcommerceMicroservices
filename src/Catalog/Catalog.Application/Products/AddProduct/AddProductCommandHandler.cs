using Catalog.Application.Abstractions.Clock;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.AddProduct;

internal sealed class AddProductCommandHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddProductCommand, long>
{
    public async Task<Result<long>> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        if (!await categoryRepository.ExistsAsync(request.CategoryId, cancellationToken))
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

        productRepository.Add(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;

    }
}
