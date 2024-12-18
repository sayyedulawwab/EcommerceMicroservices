using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;
internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id)
              .ValueGeneratedOnAdd();

        builder.Property(category => category.Name)
               .HasMaxLength(200);

        builder.Property(category => category.Description)
               .HasMaxLength(1000);

        builder.Property(category => category.ParentCategoryId);

        builder.Property(category => category.CreatedOnUtc);

        builder.Property(category => category.UpdatedOnUtc);

    }
}