using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(category => category.Id).IsClustered(false);

        builder.Property(category => category.Id)
               .HasConversion(categoryId => categoryId.Value, value => new CategoryId(value));

        builder.Property<long>("ClusterId").UseIdentityColumn();

        builder.HasIndex("ClusterId").IsClustered();

        builder.Property(category => category.Name)
               .HasMaxLength(200);

        builder.Property(category => category.Description)
               .HasMaxLength(1000);

        builder.Property(category => category.ParentCategoryId)
               .HasConversion(parentCategoryId => parentCategoryId!.Value, value => new CategoryId(value));

        builder.HasIndex(category => category.ParentCategoryId);

        builder.Property(category => category.CreatedOnUtc);

        builder.Property(category => category.UpdatedOnUtc);

    }
}
