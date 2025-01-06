using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Categories.EditCategory;
public record EditCategoryCommand(long Id, string Name, string Description, long ParentCategoryId) : ICommand<long>;