using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Categories.DeleteCategory;

internal sealed class DeleteCategoryCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<DeleteCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.Id);

        Category? category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure<Guid>(CategoryErrors.NotFound(request.Id));
        }

        if (await dbContext.Categories.AnyAsync(c => c.ParentCategoryId == categoryId, cancellationToken) ||
            await dbContext.Products.AnyAsync(p => p.CategoryId == categoryId, cancellationToken))
        {
            return Result.Failure<Guid>(CategoryErrors.InUse(request.Id));
        }

        dbContext.Categories.Remove(category);

        await dbContext.SaveChangesAsync(cancellationToken);

        return category.Id.Value;

    }
}
