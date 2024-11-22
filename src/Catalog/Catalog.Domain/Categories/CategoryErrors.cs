using SharedKernel.Domain;

namespace Catalog.Domain.Categories;
public static class CategoryErrors
{
    public static Error NoCategories => Error.NotFound(
       "Category.NoCategories",
       $"Category was not found");
    public static Error NotFound(long id) => Error.NotFound(
        "Category.NotFound",
        $"The Category with the Id = '{id}' was not found");
}
