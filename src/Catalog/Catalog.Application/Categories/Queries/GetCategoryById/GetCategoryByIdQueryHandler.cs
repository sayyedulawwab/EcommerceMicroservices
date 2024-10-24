﻿using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Abstractions;
using Catalog.Domain.Categories;

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

        var categoryResponse = new CategoryResponse()
        {
            Id = category.Id,
            Name = category.Name.Value,
            Code = category.Code.Value
        };

        return categoryResponse;
    }
}
