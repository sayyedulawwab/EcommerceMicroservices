using Bogus;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Catalog.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Data
{
    public static class DataSeeder
    {
        public static void SeedData(ApplicationDbContext context)
        {
            var tableNames = context.Model.GetEntityTypes()
                            .Select(t => t.GetTableName())
                            .Distinct()
                            .ToList();

            foreach (var tableName in tableNames)
            {
                context.Database.ExecuteSqlRaw($"TRUNCATE TABLE {tableName};");
            }

            var faker = new Faker();
           
            // Seed product categories
            for (int i = 0; i < 10; i++)
            {
                var name = new CategoryName(faker.Commerce.Department());
                var code = new CategoryCode(faker.Random.AlphaNumeric(5));

                var category = Category.Create(name, code, DateTime.UtcNow);

                context.Set<Category>().Add(category);
            }

            context.SaveChanges();

            var productCategoryGuids = context.Set<Category>().Select(pc => pc.Id).ToList();

            // Seed products
            for (int i = 0; i < 100; i++)
            {
                var name = new ProductName(faker.Commerce.ProductName());
                var description = new ProductDescription(faker.Commerce.ProductAdjective());
                var quantity = faker.Random.Number(1, 100);
                var price = new Money(faker.Finance.Amount(), Currency.Create("BDT"));
                var productCategoryId = productCategoryGuids[i % productCategoryGuids.Count];

                var product = Product.Create(name, description, price, quantity, productCategoryId, DateTime.UtcNow);

                context.Set<Product>().Add(product);
            }

            context.SaveChanges();
        }
    }
}
