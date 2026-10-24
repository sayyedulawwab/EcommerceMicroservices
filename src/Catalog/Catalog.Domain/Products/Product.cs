using Catalog.Domain.Categories;
using SharedKernel.Domain;

namespace Catalog.Domain.Products;

public sealed class Product : Entity<ProductId>
{
    private Product(ProductId id, string name, string description, Money price, int quantity, CategoryId categoryId, DateTime createdOnUtc)
        : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        Quantity = quantity;
        CategoryId = categoryId;
        CreatedOnUtc = createdOnUtc;
    }

    #pragma warning disable CS8618 // Used by EF Core, which can't bind the owned Money through the constructor
    private Product()
    {
    }
    #pragma warning restore CS8618

    public CategoryId CategoryId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public static Result<Product> Create(string name, string description, Money price, int quantity, CategoryId categoryId, DateTime createdOnUtc)
    {
        Result invariants = CheckInvariants(price, quantity);

        if (invariants.IsFailure)
        {
            return Result.Failure<Product>(invariants.Error);
        }

        return new Product(ProductId.New(), name, description, price, quantity, categoryId, createdOnUtc);
    }

    public static Result Update(Product product, string name, string description, Money price, int quantity, CategoryId categoryId, DateTime updatedOnUtc)
    {
        Result invariants = CheckInvariants(price, quantity);

        if (invariants.IsFailure)
        {
            return invariants;
        }

        product.Name = name;
        product.Description = description;
        product.Price = price;
        product.Quantity = quantity;
        product.CategoryId = categoryId;
        product.UpdatedOnUtc = updatedOnUtc;

        return Result.Success();
    }

    private static Result CheckInvariants(Money price, int quantity)
    {
        if (price.Amount <= 0)
        {
            return Result.Failure(ProductErrors.InvalidPrice());
        }

        if (quantity < 0)
        {
            return Result.Failure(ProductErrors.InvalidStockQuantity());
        }

        return Result.Success();
    }

    public bool HasStock(int quantity) => quantity > 0 && Quantity >= quantity;

    public Result ReserveStock(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(ProductErrors.InvalidQuantity(Id.Value));
        }

        if (!HasStock(quantity))
        {
            return Result.Failure(ProductErrors.InsufficientStock(Id.Value));
        }

        Quantity -= quantity;

        return Result.Success();
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        Quantity += quantity;
    }
}
