using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Configurations;
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
               .ValueGeneratedOnAdd();

        builder.Property(r => r.Token).HasMaxLength(200);

        builder.HasIndex(r => r.Token).IsUnique();

        builder.Property(r => r.ExpiresOnUtc);

        builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId);
    }
}