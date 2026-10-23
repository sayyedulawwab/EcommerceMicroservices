using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Categories.AddCategory;

internal sealed class AddCategoryCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddCategoryCommand request, CancellationToken cancellationToken)
    {
        CategoryId? parentCategoryId = request.ParentCategoryId is Guid parentId ? new CategoryId(parentId) : null;

        if (parentCategoryId is not null &&
            !await dbContext.Categories.AnyAsync(c => c.Id == parentCategoryId, cancellationToken))
        {
            return Result.Failure<Guid>(CategoryErrors.ParentNotFound(parentCategoryId.Value));
        }

        var category = Category.Create(request.Name, request.Description, parentCategoryId, dateTimeProvider.UtcNow);

        dbContext.Categories.Add(category);

        await dbContext.SaveChangesAsync(cancellationToken);

        return category.Id.Value;

    }
}
