using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain.Orders;
using SharedKernel.Domain;

namespace Ordering.Infrastructure.Configurations;
internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(orderItem => orderItem.Id);

        builder.Property(orderItem => orderItem.Id)
               .ValueGeneratedOnAdd();
        builder.Property(orderItem => orderItem.OrderId);
        builder.Property(orderItem => orderItem.ProductId);
        builder.Property(orderItem => orderItem.ProductName);
        builder.Property(orderItem => orderItem.Quantity);

        builder.OwnsOne(orderItem => orderItem.Price, priceBuilder =>
        {
            priceBuilder.Property(money => money.Amount)
                        .HasColumnType("decimal(18, 2)");

            priceBuilder.Property(money => money.Currency)
                        .HasConversion(currency => currency.Code, code => Currency.FromCode(code))
                        .HasDefaultValue(Currency.FromCode("BDT"));

        });

        builder.Property(orderItem => orderItem.CreatedOnUtc);

        //builder.HasOne<Order>()
        //    .WithMany(order => order.OrderItems)
        //    .HasForeignKey(orderItem => orderItem.OrderId);


    }
}
