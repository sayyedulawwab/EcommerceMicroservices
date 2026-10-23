using SharedKernel.Messaging;

namespace Catalog.Application.Categories.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : ICommand<Guid>;