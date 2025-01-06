using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Products;
using SharedKernel.Domain;

namespace Catalog.Application.Products.AddProduct;
internal sealed class AddProductCommandHandler : ICommandHandler<AddProductCommand, long>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<long>> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(request.Name, request.Description, new Money(request.PriceAmount, Currency.Create(request.PriceCurrency)), request.Quantity, request.CategoryId, _dateTimeProvider.UtcNow);

        _productRepository.Add(product);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;

    }
}