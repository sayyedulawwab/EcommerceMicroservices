using Catalog.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Categories.GetAllCategories;

internal sealed class GetAllCategoriesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAllCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    public async Task<Result<IReadOnlyList<CategoryResponse>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        List<CategoryResponse> categoriesResponse = await dbContext.Categories
            .AsNoTracking()
            .Select(cat => new CategoryResponse()
            {
                Id = cat.Id.Value,
                Name = cat.Name,
                Description = cat.Description,
                ParentCategoryId = cat.ParentCategoryId == null ? null : cat.ParentCategoryId.Value,
                CreatedOnUtc = cat.CreatedOnUtc,
                UpdatedOnUtc = cat.UpdatedOnUtc,
            })
            .ToListAsync(cancellationToken);

        return categoriesResponse;
    }
}