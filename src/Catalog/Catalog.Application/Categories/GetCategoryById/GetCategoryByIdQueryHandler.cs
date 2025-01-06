using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Categories;
using SharedKernel.Domain;

namespace Catalog.Application.Categories.GetCategoryById;
internal sealed class GetCategoryByIdQueryHandler : IQueryHandler<GetCategoryByIdQuery, CategoryResponse>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }
    public async Task<Result<CategoryResponse>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        Category? category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);

        if (category is null)
        {
            return Result.Failure<CategoryResponse>(CategoryErrors.NotFound(request.Id));
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