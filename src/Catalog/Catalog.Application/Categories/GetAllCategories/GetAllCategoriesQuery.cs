using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Categories.GetAllCategories;
public record GetAllCategoriesQuery() : IQuery<IReadOnlyList<CategoryResponse>>;