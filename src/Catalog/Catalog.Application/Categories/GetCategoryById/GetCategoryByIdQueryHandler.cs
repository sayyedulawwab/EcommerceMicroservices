using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Categories.GetCategoryById;

internal sealed class GetCategoryByIdQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetCategoryByIdQuery, CategoryResponse>
{
    public async Task<Result<CategoryResponse>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.Id);

        CategoryResponse? categoryResponse = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.Id == categoryId)
            .Select(category => new CategoryResponse()
            {
                Id = category.Id.Value,
                Name = category.Name,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId == null ? null : category.ParentCategoryId.Value,
                CreatedOnUtc = category.CreatedOnUtc,
                UpdatedOnUtc = category.UpdatedOnUtc,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (categoryResponse is null)
        {
            return Result.Failure<CategoryResponse>(CategoryErrors.NotFound(request.Id));
        }

        return categoryResponse;
    }
}