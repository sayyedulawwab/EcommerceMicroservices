using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
               .ValueGeneratedOnAdd();

        builder.Property(r => r.TokenHash).HasMaxLength(64);

        builder.HasIndex(r => r.TokenHash).IsUnique();

        builder.HasIndex(r => r.FamilyId);

        builder.Property(r => r.ExpiresOnUtc);
        builder.Property(r => r.CreatedOnUtc);
        builder.Property(r => r.RevokedOnUtc);

        builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId);
    }
}
