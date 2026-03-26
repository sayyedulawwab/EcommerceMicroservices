using Catalog.Application.Abstractions.Clock;
using Catalog.Domain.Categories;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Categories.AddCategory;
internal sealed class AddCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddCategoryCommand, long>
{
    public async Task<Result<long>> Handle(AddCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = Category.Create(request.Name, request.Description, request.ParentCategoryId, dateTimeProvider.UtcNow);

        categoryRepository.Add(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return category.Id;

    }
}