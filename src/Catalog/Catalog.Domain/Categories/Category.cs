using Catalog.Domain.Abstractions;
using Catalog.Domain.Categories.Events;

namespace Catalog.Domain.Categories;

public sealed class Category : Entity<long>
{
    private Category(CategoryName name, CategoryCode code, DateTime createdOn)
    {
        Name = name;
        Code = code;
        CreatedOn = createdOn;
    }
    private Category()
    {
    }

    public CategoryName Name { get; private set; }
    public CategoryCode Code { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }


    public static Category Create(CategoryName name, CategoryCode code, DateTime createdOn)
    {
        var category = new Category(name, code, createdOn);

        category.RaiseDomainEvent(new CategoryCreatedDomainEvent(category.Id));

        return category;
    }

    public static Category Update(Category category, CategoryName name, CategoryCode code, DateTime updatedOn)
    {
        category.Name = name;
        category.Code = code;
        category.UpdatedOn = updatedOn;

        return category;
    }
}