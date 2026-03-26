using SharedKernel.Messaging;

namespace Catalog.Application.Categories.EditCategory;

public record EditCategoryCommand(long Id, string Name, string Description, long ParentCategoryId) : ICommand<long>;