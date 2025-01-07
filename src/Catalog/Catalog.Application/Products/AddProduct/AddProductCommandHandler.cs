using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Products;
using SharedKernel.Domain;

namespace Catalog.Application.Products.AddProduct;
internal sealed class AddProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddProductCommand, long>
{
    public async Task<Result<long>> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(
            request.Name,
            request.Description,
            new Money(request.PriceAmount, Currency.Create(request.PriceCurrency)),
            request.Quantity,
            request.CategoryId,
            dateTimeProvider.UtcNow);

        productRepository.Add(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;

    }
}