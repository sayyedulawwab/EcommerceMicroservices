using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Categories.AddCategory;
public record AddCategoryCommand(string name, string description, long parentCategoryId) : ICommand<long>;