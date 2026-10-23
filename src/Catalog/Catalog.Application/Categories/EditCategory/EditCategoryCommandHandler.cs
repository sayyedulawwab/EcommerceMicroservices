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
    : ICommandHandler<EditCategoryCommand, long>
{
    private const int MaxCategoryDepth = 100;

    public async Task<Result<long>> Handle(EditCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            return Result.Failure<long>(CategoryErrors.NotFound(request.Id));
        }

        if (request.ParentCategoryId is long parentCategoryId)
        {
            Result parentResult = await ValidateParentAsync(request.Id, parentCategoryId, cancellationToken);

            if (parentResult.IsFailure)
            {
                return Result.Failure<long>(parentResult.Error);
            }
        }

        Category.Update(category, request.Name, request.Description, request.ParentCategoryId, dateTimeProvider.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return category.Id;

    }

    // Walks up from the new parent; reaching the edited category means the change would create a cycle.
    private async Task<Result> ValidateParentAsync(long categoryId, long parentCategoryId, CancellationToken cancellationToken)
    {
        long? currentId = parentCategoryId;

        for (int depth = 0; currentId is long id && depth < MaxCategoryDepth; depth++)
        {
            if (id == categoryId)
            {
                return Result.Failure(CategoryErrors.CircularParent(categoryId));
            }

            var current = await dbContext.Categories
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new { c.ParentCategoryId })
                .FirstOrDefaultAsync(cancellationToken);

            if (current is null)
            {
                return depth == 0
                    ? Result.Failure(CategoryErrors.ParentNotFound(parentCategoryId))
                    : Result.Success();
            }

            currentId = current.ParentCategoryId;
        }

        return currentId is null
            ? Result.Success()
            : Result.Failure(CategoryErrors.CircularParent(categoryId));
    }
}
