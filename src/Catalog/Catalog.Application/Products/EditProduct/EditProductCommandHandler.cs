using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Products;
using SharedKernel.Domain;

namespace Catalog.Application.Products.EditProduct;
internal sealed class EditProductCommandHandler : ICommandHandler<EditProductCommand, long>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EditProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<long>> Handle(EditProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);

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
            _dateTimeProvider.UtcNow);

        _productRepository.Update(product);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;

    }
}