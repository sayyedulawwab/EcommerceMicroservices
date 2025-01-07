using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Products;
using SharedKernel.Domain;

namespace Catalog.Application.Products.EditProduct;
internal sealed class EditProductCommandHandler(
    IProductRepository productRepository,
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

        product = Product.Update(
            product,
            request.Name,
            request.Description,
            new Money(request.PriceAmount, Currency.Create(request.PriceCurrency)),
            request.Quantity,
            request.CategoryId,
            dateTimeProvider.UtcNow);

        productRepository.Update(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;

    }
}