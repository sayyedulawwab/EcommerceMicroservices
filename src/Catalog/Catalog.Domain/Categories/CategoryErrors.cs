using SharedKernel.Domain;

namespace Catalog.Domain.Categories;

public static class CategoryErrors
{
    public static Error NoCategories => Error.NotFound(
       "Category.NoCategories",
       $"Category was not found");

    public static Error NotFound(Guid id) => Error.NotFound(
        "Category.NotFound",
        $"The Category with the Id = '{id}' was not found");

    public static Error ParentNotFound(Guid parentCategoryId) => Error.BadRequest(
        "Category.ParentNotFound",
        $"The parent category with the Id = '{parentCategoryId}' does not exist");

    public static Error CircularParent(Guid id) => Error.BadRequest(
        "Category.CircularParent",
        $"Category '{id}' can not be its own parent or a descendant of itself");

    public static Error InUse(Guid id) => Error.Conflict(
        "Category.InUse",
        $"Category '{id}' still has products or child categories");
}
