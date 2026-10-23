using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Configurations;

internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("UserTokens");

        builder.HasKey(t => t.Id).IsClustered(false);

        builder.Property(t => t.Id)
               .HasConversion(id => id.Value, value => new UserTokenId(value));

        builder.Property<long>("ClusterId").UseIdentityColumn();

        builder.HasIndex("ClusterId").IsClustered();

        builder.Property(t => t.UserId)
               .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(t => t.Purpose);

        builder.Property(t => t.TokenHash).HasMaxLength(64);

        builder.HasIndex(t => new { t.UserId, t.Purpose, t.TokenHash });

        builder.Property(t => t.ExpiresOnUtc);
        builder.Property(t => t.CreatedOnUtc);
        builder.Property(t => t.ConsumedOnUtc);

        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId);
    }
}
