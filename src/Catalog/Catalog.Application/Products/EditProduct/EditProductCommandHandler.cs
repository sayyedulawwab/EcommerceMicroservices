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
        var product = await _productRepository.GetByIdAsync(request.id);

        if (product is null)
        {
            return Result.Failure<long>(ProductErrors.NotFound());
        }

        product = Product.Update(product, request.name, request.description, new Money(request.priceAmount, Currency.Create(request.priceCurrency)), request.quantity, request.categoryId, _dateTimeProvider.UtcNow);

        _productRepository.Update(product);

        await _unitOfWork.SaveChangesAsync();

        return product.Id;

    }
}