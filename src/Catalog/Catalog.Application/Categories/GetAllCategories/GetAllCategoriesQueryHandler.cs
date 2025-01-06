﻿using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Categories;
using SharedKernel.Domain;

namespace Catalog.Application.Categories.GetAllCategories;
internal sealed class GetAllCategoriesQueryHandler : IQueryHandler<GetAllCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    private readonly ICategoryRepository _productCategoryRepository;
    public GetAllCategoriesQueryHandler(ICategoryRepository productCategoryRepository)
    {
        _productCategoryRepository = productCategoryRepository;
    }
    public async Task<Result<IReadOnlyList<CategoryResponse>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {

        IReadOnlyList<Category> categories = await _productCategoryRepository.GetAllAsync(cancellationToken);

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