using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Categories.EditCategory;

internal sealed class EditCategoryCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<EditCategoryCommand, Guid>
{
    private const int MaxCategoryDepth = 100;

    public async Task<Result<Guid>> Handle(EditCategoryCommand request, CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.Id);

        Category? category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure<Guid>(CategoryErrors.NotFound(request.Id));
        }

        CategoryId? parentCategoryId = request.ParentCategoryId is Guid parentId ? new CategoryId(parentId) : null;

        if (parentCategoryId is not null)
        {
            Result parentResult = await ValidateParentAsync(categoryId, parentCategoryId, cancellationToken);

            if (parentResult.IsFailure)
            {
                return Result.Failure<Guid>(parentResult.Error);
            }
        }

        Category.Update(category, request.Name, request.Description, parentCategoryId, dateTimeProvider.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return category.Id.Value;

    }

    // Walks up from the new parent; reaching the edited category means the change would create a cycle.
    private async Task<Result> ValidateParentAsync(CategoryId categoryId, CategoryId parentCategoryId, CancellationToken cancellationToken)
    {
        CategoryId? currentId = parentCategoryId;

        for (int depth = 0; currentId is { } id && depth < MaxCategoryDepth; depth++)
        {
            if (id == categoryId)
            {
                return Result.Failure(CategoryErrors.CircularParent(categoryId.Value));
            }

            var current = await dbContext.Categories
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new { c.ParentCategoryId })
                .FirstOrDefaultAsync(cancellationToken);

            if (current is null)
            {
                return depth == 0
                    ? Result.Failure(CategoryErrors.ParentNotFound(parentCategoryId.Value))
                    : Result.Success();
            }

            currentId = current.ParentCategoryId;
        }

        return currentId is null
            ? Result.Success()
            : Result.Failure(CategoryErrors.CircularParent(categoryId.Value));
    }
}
