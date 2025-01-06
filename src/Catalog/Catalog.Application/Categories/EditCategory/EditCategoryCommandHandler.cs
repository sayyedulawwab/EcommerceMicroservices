using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Categories;
using SharedKernel.Domain;

namespace Catalog.Application.Categories.EditCategory;
internal sealed class EditCategoryCommandHandler : ICommandHandler<EditCategoryCommand, long>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EditCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<long>> Handle(EditCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? productCategory = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);

        if (productCategory is null)
        {
            return Result.Failure<long>(CategoryErrors.NotFound(request.Id));
        }

        productCategory = Category.Update(productCategory, request.Name, request.Description, request.ParentCategoryId, _dateTimeProvider.UtcNow);

        _categoryRepository.Update(productCategory);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return productCategory.Id;

    }
}