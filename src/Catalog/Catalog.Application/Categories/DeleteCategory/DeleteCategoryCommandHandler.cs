using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Abstractions;
using Catalog.Domain.Categories;

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
        var category = await _categoryRepository.GetByIdAsync(request.id);

        _categoryRepository.Remove(category);

        await _unitOfWork.SaveChangesAsync();

        return category.Id;

    }
}
