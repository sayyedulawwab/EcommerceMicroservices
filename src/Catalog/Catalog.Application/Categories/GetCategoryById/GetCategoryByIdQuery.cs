using SharedKernel.Messaging;

namespace Catalog.Application.Categories.GetCategoryById;

public record GetCategoryByIdQuery(Guid Id) : IQuery<CategoryResponse>;