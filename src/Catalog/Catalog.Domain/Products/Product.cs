using Catalog.Domain.Abstractions;
using Catalog.Domain.Shared;

namespace Catalog.Domain.Products;

public sealed class Product : Entity<long>
{
    private Product(string name, string description, Money price, int quantity, long categoryId, DateTime createdOn)
    {
        Name = name;
        Description = description;
        Price = price;
        Quantity = quantity;
        CategoryId = categoryId;
        CreatedOn = createdOn;
    }

    private Product()
    {
    }

    public long CategoryId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    public static Product Create(string name, string description, Money price, int quantity, long categoryId, DateTime createdOn)
    {
        var product = new Product(name, description, price, quantity, categoryId, createdOn);

        return product;
    }


    public static Product Update(Product product, string name, string description, Money price, int quantity, long categoryId, DateTime updatedOn)
    {

        product.Name = name;
        product.Description = description;
        product.Price = price;
        product.Quantity = quantity;
        product.CategoryId = categoryId;
        product.UpdatedOn = updatedOn;

        return product;
    }

    public void RemoveStock(int quantityDesired)
    {
        int removed = Math.Min(quantityDesired, this.Quantity);
        this.Quantity -= removed;
    }

}