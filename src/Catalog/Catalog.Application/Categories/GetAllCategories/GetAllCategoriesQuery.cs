using SharedKernel.Messaging;

namespace Catalog.Application.Categories.GetAllCategories;

public record GetAllCategoriesQuery() : IQuery<IReadOnlyList<CategoryResponse>>;