using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Categories;
using SharedKernel.Domain;

namespace Catalog.Application.Categories.GetAllCategories;
internal sealed class GetAllCategoriesQueryHandler(ICategoryRepository categoryRepository)
    : IQueryHandler<GetAllCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    public async Task<Result<IReadOnlyList<CategoryResponse>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Category> categories = await categoryRepository.GetAllAsync(cancellationToken);

        var categoriesResponse = categories.Select(cat => new CategoryResponse()
        {
            Id = cat.Id,
            Name = cat.Name,
            Description = cat.Description,
            ParentCategoryId = cat.ParentCategoryId,
            CreatedOnUtc = cat.CreatedOnUtc,
            UpdatedOnUtc = cat.UpdatedOnUtc,
        }).ToList();

        return categoriesResponse;
    }
}