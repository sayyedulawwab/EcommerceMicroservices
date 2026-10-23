using SharedKernel.Messaging;

namespace Catalog.Application.Categories.EditCategory;

public record EditCategoryCommand(Guid Id, string Name, string Description, Guid? ParentCategoryId) : ICommand<Guid>;
