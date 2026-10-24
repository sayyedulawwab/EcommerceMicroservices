using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(r => r.Id).IsClustered(false);

        builder.Property(r => r.Id)
               .HasConversion(id => id.Value, value => new RefreshTokenId(value));

        builder.Property<long>("ClusterId").UseIdentityColumn();

        builder.HasIndex("ClusterId").IsClustered();

        builder.Property(r => r.UserId)
               .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(r => r.TokenHash).HasMaxLength(64);

        builder.HasIndex(r => r.TokenHash).IsUnique();

        builder.HasIndex(r => r.FamilyId);

        builder.Property(r => r.ExpiresOnUtc);
        builder.Property(r => r.CreatedOnUtc);
        builder.Property(r => r.RevokedOnUtc);

        builder.Property<byte[]>("RowVersion")
               .IsRowVersion();

        builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId);
    }
}
