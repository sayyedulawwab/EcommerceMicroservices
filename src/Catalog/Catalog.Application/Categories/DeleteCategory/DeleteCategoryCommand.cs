using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Categories.DeleteCategory;
public record DeleteCategoryCommand(long Id) : ICommand<long>;