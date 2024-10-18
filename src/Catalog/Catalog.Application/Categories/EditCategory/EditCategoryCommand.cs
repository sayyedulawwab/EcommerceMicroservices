using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Categories.EditCategory;
public record EditCategoryCommand(long id, string name, string code) : ICommand<long>;
