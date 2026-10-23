using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Categories.DeleteCategory;

internal sealed class DeleteCategoryCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<DeleteCategoryCommand, long>
{
    public async Task<Result<long>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            return Result.Failure<long>(CategoryErrors.NotFound(request.Id));
        }

        if (await dbContext.Categories.AnyAsync(c => c.ParentCategoryId == request.Id, cancellationToken) ||
            await dbContext.Products.AnyAsync(p => p.CategoryId == request.Id, cancellationToken))
        {
            return Result.Failure<long>(CategoryErrors.InUse(request.Id));
        }

        dbContext.Categories.Remove(category);

        await dbContext.SaveChangesAsync(cancellationToken);

        return category.Id;

    }
}
