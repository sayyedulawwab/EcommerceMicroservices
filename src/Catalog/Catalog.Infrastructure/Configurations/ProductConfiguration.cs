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

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
               .ValueGeneratedOnAdd();

        builder.Property(product => product.Name)
               .HasMaxLength(200);

        builder.Property(product => product.Description)
               .HasMaxLength(1000);

        builder.Property(product => product.Quantity);

        builder.OwnsOne(product => product.Price, priceBuilder =>
        {
            priceBuilder.Property(money => money.Amount)
                        .HasColumnType("decimal(18, 2)");

            priceBuilder.Property(money => money.Currency)
                        .HasConversion(currency => currency.Code, code => Currency.FromCode(code))
                        .HasDefaultValue(Currency.FromCode("BDT"));

        });

        builder.Property(product => product.CreatedOnUtc);
        builder.Property(product => product.UpdatedOnUtc);

        //builder.HasOne<Category>()
        //       .WithMany()
        //       .HasForeignKey(product => product.CategoryId);

    }
}
