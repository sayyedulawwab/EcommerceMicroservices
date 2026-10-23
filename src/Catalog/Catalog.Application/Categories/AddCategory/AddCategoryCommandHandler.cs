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
    : ICommandHandler<AddCategoryCommand, long>
{
    public async Task<Result<long>> Handle(AddCategoryCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentCategoryId is long parentCategoryId &&
            !await dbContext.Categories.AnyAsync(c => c.Id == parentCategoryId, cancellationToken))
        {
            return Result.Failure<long>(CategoryErrors.ParentNotFound(parentCategoryId));
        }

        var category = Category.Create(request.Name, request.Description, request.ParentCategoryId, dateTimeProvider.UtcNow);

        dbContext.Categories.Add(category);

        await dbContext.SaveChangesAsync(cancellationToken);

        return category.Id;

    }
}
