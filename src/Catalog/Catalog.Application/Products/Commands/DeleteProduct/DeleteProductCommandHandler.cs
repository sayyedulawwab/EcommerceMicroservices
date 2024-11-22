using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Products;
using SharedKernel.Domain;

namespace Catalog.Application.Products.Commands.DeleteProduct;
internal sealed class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, long>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<long>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.id);

        if (product is null)
        {
            return Result.Failure<long>(ProductErrors.NotFound());
        }

        _productRepository.Remove(product);

        await _unitOfWork.SaveChangesAsync();

        return product.Id;

    }
}