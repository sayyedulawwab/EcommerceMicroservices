using SharedKernel.Domain;

namespace Catalog.Domain.Products;

public sealed class Product : Entity<long>
{
    private Product(string name, string description, Money price, int quantity, long categoryId, DateTime createdOnUtc)
    {
        Name = name;
        Description = description;
        Price = price;
        Quantity = quantity;
        CategoryId = categoryId;
        CreatedOnUtc = createdOnUtc;
    }

    private Product()
    {
    }

    public long CategoryId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public static Product Create(string name, string description, Money price, int quantity, long categoryId, DateTime createdOnUtc)
    {
        var product = new Product(name, description, price, quantity, categoryId, createdOnUtc);

        return product;
    }


    public static Product Update(Product product, string name, string description, Money price, int quantity, long categoryId, DateTime updatedOnUtc)
    {

        product.Name = name;
        product.Description = description;
        product.Price = price;
        product.Quantity = quantity;
        product.CategoryId = categoryId;
        product.UpdatedOnUtc = updatedOnUtc;

        return product;
    }

    public void RemoveStock(int quantityDesired)
    {
        int removed = Math.Min(quantityDesired, this.Quantity);
        this.Quantity -= removed;
    }

}