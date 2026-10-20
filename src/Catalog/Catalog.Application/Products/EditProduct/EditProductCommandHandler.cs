using Catalog.Application.Abstractions.Clock;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.EditProduct;

internal sealed class EditProductCommandHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<EditProductCommand, long>
{
    public async Task<Result<long>> Handle(EditProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await productRepository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
        {
            return Result.Failure<long>(ProductErrors.NotFound());
        }

        if (product.CategoryId != request.CategoryId &&
            !await categoryRepository.ExistsAsync(request.CategoryId, cancellationToken))
        {
            return Result.Failure<long>(ProductErrors.CategoryNotFound(request.CategoryId));
        }

        product = Product.Update(
            product,
            request.Name,
            request.Description,
            new Money(request.PriceAmount, Currency.FromCode(request.PriceCurrency)),
            request.Quantity,
            request.CategoryId,
            dateTimeProvider.UtcNow);

        productRepository.Update(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;

    }
}
