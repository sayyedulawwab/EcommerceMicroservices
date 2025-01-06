using Bogus;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;

namespace Catalog.Infrastructure.Data;

public static class DataSeeder
{
    public static void SeedData(ApplicationDbContext context)
    {
        var faker = new Faker();

        // Seed product categories
        for (int i = 0; i < 10; i++)
        {
            string name = faker.Commerce.Department();
            string description = faker.Commerce.Department();
            int parentCategoryId = faker.Random.Int();

            var category = Category.Create(name, description, parentCategoryId, DateTime.UtcNow);

            context.Set<Category>().Add(category);
        }

        context.SaveChanges();

        var productCategoryGuids = context.Set<Category>().Select(pc => pc.Id).ToList();

        // Seed products
        for (int i = 0; i < 100; i++)
        {
            string name = faker.Commerce.ProductName();
            string description = faker.Commerce.ProductAdjective();
            int quantity = faker.Random.Number(1, 100);
            var price = new Money(faker.Finance.Amount(), Currency.Create("BDT"));
            long productCategoryId = productCategoryGuids[i % productCategoryGuids.Count];

            var product = Product.Create(name, description, price, quantity, productCategoryId, DateTime.UtcNow);

            context.Set<Product>().Add(product);
        }

        context.SaveChanges();
    }
}
