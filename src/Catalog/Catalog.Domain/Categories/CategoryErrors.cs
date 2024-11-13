using Catalog.Domain.Abstractions;

namespace Catalog.Domain.Categories;
public static class CategoryErrors
{
    public static Error NoCategories => new(
       "Category.NoCategories",
       $"Category was not found");
    public static Error NotFound(long id) => new(
        "Category.NotFound",
        $"The Category with the Id = '{id}' was not found");
}
