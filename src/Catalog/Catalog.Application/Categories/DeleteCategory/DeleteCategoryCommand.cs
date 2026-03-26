using SharedKernel.Messaging;

namespace Catalog.Application.Categories.DeleteCategory;

public record DeleteCategoryCommand(long Id) : ICommand<long>;