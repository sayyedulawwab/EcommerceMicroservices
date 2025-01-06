using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Categories;
using SharedKernel.Domain;

namespace Catalog.Application.Categories.DeleteCategory;
internal sealed class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand, long>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<long>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);

        if (category is null)
        {
            return Result.Failure<long>(CategoryErrors.NotFound(request.Id));
        }

        _categoryRepository.Remove(category);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category.Id;

    }
}