using Catalog.Domain.Categories;

namespace Catalog.Domain.UnitTests.Categories;

public sealed class CategoryTests
{
    private static readonly DateTime CreatedOn = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UpdatedOn = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly CategoryId ParentId = CategoryId.New();

    [Fact]
    public void Create_NullParent_CreatesRootCategory()
    {
        var category = Category.Create("Electronics", "Electronic devices", null, CreatedOn);

        Assert.Equal("Electronics", category.Name);
        Assert.Equal("Electronic devices", category.Description);
        Assert.Null(category.ParentCategoryId);
        Assert.Equal(CreatedOn, category.CreatedOnUtc);
        Assert.Null(category.UpdatedOnUtc);
    }

    [Fact]
    public void Create_WithParent_SetsParent()
    {
        var category = Category.Create("Laptops", "Portable computers", ParentId, CreatedOn);

        Assert.Equal(ParentId, category.ParentCategoryId);
    }

    [Fact]
    public void Update_NullParent_ClearsParentAndSetsFields()
    {
        var category = Category.Create("Laptops", "Portable computers", ParentId, CreatedOn);

        var updated = Category.Update(category, "Notebooks", "Thin laptops", null, UpdatedOn);

        Assert.Same(category, updated);
        Assert.Equal("Notebooks", updated.Name);
        Assert.Equal("Thin laptops", updated.Description);
        Assert.Null(updated.ParentCategoryId);
        Assert.Equal(CreatedOn, updated.CreatedOnUtc);
        Assert.Equal(UpdatedOn, updated.UpdatedOnUtc);
    }

    [Fact]
    public void Update_WithParent_SetsParent()
    {
        var category = Category.Create("Laptops", "Portable computers", null, CreatedOn);

        var updated = Category.Update(category, "Laptops", "Portable computers", ParentId, UpdatedOn);

        Assert.Equal(ParentId, updated.ParentCategoryId);
    }
}
