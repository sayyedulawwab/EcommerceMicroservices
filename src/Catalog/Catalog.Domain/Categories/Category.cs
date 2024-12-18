using SharedKernel.Domain;

namespace Catalog.Domain.Categories;

public sealed class Category : Entity<long>
{
    private Category(string name, string description, long parentCategoryId, DateTime createdOnUtc)
    {
        Name = name;
        Description = description;
        ParentCategoryId = parentCategoryId;
        CreatedOnUtc = createdOnUtc;
    }
    private Category()
    {
    }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public long ParentCategoryId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public static Category Create(string name, string description, long parentCategoryId, DateTime createdOn)
    {
        var category = new Category(name, description, parentCategoryId, createdOn);

        return category;
    }

    public static Category Update(Category category, string name, string description, long parentCategoryId, DateTime updatedOn)
    {
        category.Name = name;
        category.Description = description;
        category.ParentCategoryId = parentCategoryId;
        category.UpdatedOnUtc = updatedOn;

        return category;
    }
}