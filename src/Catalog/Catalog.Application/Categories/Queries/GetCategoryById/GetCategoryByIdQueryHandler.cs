using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Categories;
using SharedKernel.Domain;

namespace Catalog.Application.Categories.Queries.GetCategoryById;
internal sealed class GetCategoryByIdQueryHandler : IQueryHandler<GetCategoryByIdQuery, CategoryResponse>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }
    public async Task<Result<CategoryResponse>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.id, cancellationToken);

        if (category is null)
        {
            return Result.Failure<CategoryResponse>(CategoryErrors.NotFound(request.id));
        }

        var categoryResponse = new CategoryResponse()
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ParentCategoryId = category.ParentCategoryId,
            CreatedOnUtc = category.CreatedOnUtc,
            UpdatedOnUtc = category.UpdatedOnUtc,
        };

        return categoryResponse;
    }
}