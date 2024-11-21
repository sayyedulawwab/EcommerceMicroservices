using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Categories.Commands.DeleteCategory;
public record DeleteCategoryCommand(long id) : ICommand<long>;