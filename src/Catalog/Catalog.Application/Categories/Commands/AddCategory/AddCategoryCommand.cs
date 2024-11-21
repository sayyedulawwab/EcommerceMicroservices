using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Categories.Commands.AddCategory;
public record AddCategoryCommand(string name, string description, long parentCategoryId) : ICommand<long>;