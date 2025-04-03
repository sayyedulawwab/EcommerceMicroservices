using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Configurations;
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
               .ValueGeneratedOnAdd();

        builder.Property(user => user.FirstName)
               .HasMaxLength(200);

        builder.Property(user => user.LastName)
               .HasMaxLength(200);


        builder.Property(user => user.Email)
               .HasMaxLength(200);

        builder.Property(user => user.PasswordHash);
        builder.Property(user => user.IsAdmin);

        builder.Property(user => user.CreatedOnUtc);

        builder.Property(user => user.UpdatedOnUtc);
    }
}
