using SharedKernel.Domain;

namespace Catalog.Domain.Categories;

public sealed class Category : Entity<CategoryId>
{
    private Category(CategoryId id, string name, string description, CategoryId? parentCategoryId, DateTime createdOnUtc)
        : base(id)
    {
        Name = name;
        Description = description;
        ParentCategoryId = parentCategoryId;
        CreatedOnUtc = createdOnUtc;
    }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public CategoryId? ParentCategoryId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public static Category Create(string name, string description, CategoryId? parentCategoryId, DateTime createdOn)
    {
        var category = new Category(CategoryId.New(), name, description, parentCategoryId, createdOn);

        return category;
    }

    public static Category Update(Category category, string name, string description, CategoryId? parentCategoryId, DateTime updatedOn)
    {
        category.Name = name;
        category.Description = description;
        category.ParentCategoryId = parentCategoryId;
        category.UpdatedOnUtc = updatedOn;

        return category;
    }
}
