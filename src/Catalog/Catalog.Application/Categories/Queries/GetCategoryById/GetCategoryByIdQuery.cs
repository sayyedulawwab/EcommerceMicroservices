using Catalog.Application.Abstractions.Messaging;
namespace Catalog.Application.Categories.Queries.GetCategoryById;

public record GetCategoryByIdQuery(long id) : IQuery<CategoryResponse>;