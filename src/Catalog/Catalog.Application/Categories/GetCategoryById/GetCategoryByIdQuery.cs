using Catalog.Application.Abstractions.Messaging;
namespace Catalog.Application.Categories.GetCategoryById;

public record GetCategoryByIdQuery(long id) : IQuery<CategoryResponse>;