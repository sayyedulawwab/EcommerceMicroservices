﻿using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Abstractions;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;

namespace Catalog.Application.Categories.Queries.GetAllCategories;
internal sealed class GetAllCategoriesQueryHandler : IQueryHandler<GetAllCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    private readonly ICategoryRepository _productCategoryRepository;
    public GetAllCategoriesQueryHandler(ICategoryRepository productCategoryRepository)
    {
        _productCategoryRepository = productCategoryRepository;
    }
    public async Task<Result<IReadOnlyList<CategoryResponse>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {

        var categories = await _productCategoryRepository.GetAllAsync();

        if (categories is null)
        {
            return Result.Failure<IReadOnlyList<CategoryResponse>>(CategoryErrors.NoCategories);
        }

        var categoriesResponse = categories.Select(cat => new CategoryResponse()
        {
            Id = cat.Id,
            Name = cat.Name,
            Description = cat.Description,
            ParentCategoryId = cat.ParentCategoryId,
            CreatedOn = cat.CreatedOn,
            UpdatedOn = cat.UpdatedOn,
        }).ToList();

        return categoriesResponse;
    }
}