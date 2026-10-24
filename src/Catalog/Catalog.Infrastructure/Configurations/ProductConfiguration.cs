using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Domain;

namespace Catalog.Infrastructure.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(product => product.Id).IsClustered(false);

        builder.Property(product => product.Id)
               .HasConversion(productId => productId.Value, value => new ProductId(value));

        builder.Property<long>("ClusterId").UseIdentityColumn();

        builder.HasIndex("ClusterId").IsClustered();

        builder.Property(product => product.CategoryId)
               .HasConversion(categoryId => categoryId.Value, value => new CategoryId(value));

        builder.Property(product => product.Name)
               .HasMaxLength(200);

        builder.Property(product => product.Description)
               .HasMaxLength(1000);

        builder.Property(product => product.Quantity);

        builder.HasIndex(product => product.CategoryId);

        builder.OwnsOne(product => product.Price, priceBuilder =>
        {
            priceBuilder.Property(money => money.Amount)
                        .HasColumnType("decimal(18, 2)");

            priceBuilder.Property(money => money.Currency)
                        .HasConversion(currency => currency.Code, code => Currency.FromStoredCode(code))
                        .HasDefaultValue(Currency.Bdt);

        });

        builder.Property(product => product.CreatedOnUtc);
        builder.Property(product => product.UpdatedOnUtc);

        // Concurrent stock reservations for the same product must not overwrite each other.
        builder.Property<byte[]>("RowVersion")
               .IsRowVersion();

        //builder.HasOne<Category>()
        //       .WithMany()
        //       .HasForeignKey(product => product.CategoryId);

    }
}
