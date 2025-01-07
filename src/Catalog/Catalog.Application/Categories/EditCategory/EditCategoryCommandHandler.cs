using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Categories;
using SharedKernel.Domain;

namespace Catalog.Application.Categories.EditCategory;
internal sealed class EditCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<EditCategoryCommand, long>
{
    public async Task<Result<long>> Handle(EditCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken);

        if (category is null)
        {
            return Result.Failure<long>(CategoryErrors.NotFound(request.Id));
        }

        category = Category.Update(category, request.Name, request.Description, request.ParentCategoryId, dateTimeProvider.UtcNow);

        categoryRepository.Update(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return category.Id;

    }
}