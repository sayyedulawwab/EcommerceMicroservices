using SharedKernel.Messaging;

namespace Catalog.Application.Categories.AddCategory;
public record AddCategoryCommand(string Name, string Description, long ParentCategoryId) : ICommand<long>;