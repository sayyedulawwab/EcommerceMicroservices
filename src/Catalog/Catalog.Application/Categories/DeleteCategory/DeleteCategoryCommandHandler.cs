using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Categories.DeleteCategory;

internal sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteCategoryCommand, long>
{
    public async Task<Result<long>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken);

        if (category is null)
        {
            return Result.Failure<long>(CategoryErrors.NotFound(request.Id));
        }

        if (await categoryRepository.HasChildrenAsync(request.Id, cancellationToken) ||
            await productRepository.AnyInCategoryAsync(request.Id, cancellationToken))
        {
            return Result.Failure<long>(CategoryErrors.InUse(request.Id));
        }

        categoryRepository.Remove(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return category.Id;

    }
}
