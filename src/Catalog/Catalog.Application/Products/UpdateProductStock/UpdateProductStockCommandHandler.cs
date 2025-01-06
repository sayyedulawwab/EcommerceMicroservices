using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Products;
using SharedKernel.Domain;

namespace Catalog.Application.Products.UpdateProductStock;
internal sealed class UpdateProductStockCommandHandler : ICommandHandler<UpdateProductStockCommand, long>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductStockCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<long>> Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        Product? product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
        {
            return Result.Failure<long>(ProductErrors.NotFound());
        }

        if (product.Quantity == 0)
        {
            return Result.Failure<long>(ProductErrors.SoldOut(request.Id));
        }

        product.RemoveStock(request.Quantity);

        _productRepository.Update(product);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;

    }
}