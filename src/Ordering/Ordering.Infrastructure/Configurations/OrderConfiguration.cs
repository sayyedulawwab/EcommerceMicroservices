using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain.Orders;
using SharedKernel.Domain;

namespace Ordering.Infrastructure.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.Id)
               .ValueGeneratedOnAdd();

        builder.Property(order => order.Status);

        builder.Property(order => order.CancellationReason)
               .HasMaxLength(Order.MaxCancellationReasonLength);

        builder.HasIndex(order => order.UserId);

        builder.OwnsOne(order => order.TotalPrice, priceBuilder =>
        {
            priceBuilder.Property(money => money.Amount)
                        .HasColumnType("decimal(18, 2)");

            priceBuilder.Property(money => money.Currency)
                        .HasConversion(currency => currency.Code, code => Currency.FromCode(code))
                        .HasDefaultValue(Currency.FromCode("BDT"));

        });

        builder.Property(order => order.CreatedOnUtc);
        builder.Property(order => order.UpdatedOnUtc);

        builder.HasMany(order => order.OrderItems)
               .WithOne()
               .HasForeignKey(orderItem => orderItem.OrderId);

        // A payment result and a customer cancellation can race on the same order; the loser retries or gets a 409.
        builder.Property<byte[]>("RowVersion")
               .IsRowVersion();
    }
}
