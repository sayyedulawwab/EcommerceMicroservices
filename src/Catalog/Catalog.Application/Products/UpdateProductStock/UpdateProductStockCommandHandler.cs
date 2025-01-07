using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Products;
using SharedKernel.Domain;

namespace Catalog.Application.Products.UpdateProductStock;
internal sealed class UpdateProductStockCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProductStockCommand, long>
{
    public async Task<Result<long>> Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        Product? product = await productRepository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
        {
            return Result.Failure<long>(ProductErrors.NotFound());
        }

        if (product.Quantity == 0)
        {
            return Result.Failure<long>(ProductErrors.SoldOut(request.Id));
        }

        product.RemoveStock(request.Quantity);

        productRepository.Update(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;

    }
}