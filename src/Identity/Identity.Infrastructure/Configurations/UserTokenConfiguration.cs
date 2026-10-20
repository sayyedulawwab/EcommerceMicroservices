using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Configurations;

internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("UserTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
               .ValueGeneratedOnAdd();

        builder.Property(t => t.Purpose);

        builder.Property(t => t.TokenHash).HasMaxLength(64);

        builder.HasIndex(t => new { t.UserId, t.Purpose, t.TokenHash });

        builder.Property(t => t.ExpiresOnUtc);
        builder.Property(t => t.CreatedOnUtc);
        builder.Property(t => t.ConsumedOnUtc);

        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId);
    }
}
