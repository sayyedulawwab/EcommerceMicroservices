using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Abstractions;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Catalog.Domain.Shared;

namespace Catalog.Application.Products.Commands.AddProduct;
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
        var product = Product.Create(request.name, request.description, new Money(request.priceAmount, Currency.Create(request.priceCurrency)), request.quantity, request.categoryId, _dateTimeProvider.UtcNow);

        _productRepository.Add(product);

        await _unitOfWork.SaveChangesAsync();

        return product.Id;

    }
}