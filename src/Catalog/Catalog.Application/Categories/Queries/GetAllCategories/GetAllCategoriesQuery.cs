using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Categories.Queries.GetAllCategories;
public record GetAllCategoriesQuery() : IQuery<IReadOnlyList<CategoryResponse>>;