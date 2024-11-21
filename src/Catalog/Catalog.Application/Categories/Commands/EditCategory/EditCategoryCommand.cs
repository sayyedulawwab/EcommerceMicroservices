using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Categories.Commands.EditCategory;
public record EditCategoryCommand(long id, string name, string description, long parentCategoryId) : ICommand<long>;