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

        builder.HasKey(orderItem => orderItem.Id).IsClustered(false);

        builder.Property(orderItem => orderItem.Id)
               .HasConversion(orderItemId => orderItemId.Value, value => new OrderItemId(value));

        builder.Property<long>("ClusterId").UseIdentityColumn();
        builder.HasIndex("ClusterId").IsClustered();

        builder.Property(orderItem => orderItem.OrderId)
               .HasConversion(orderId => orderId.Value, value => new OrderId(value));
        builder.Property(orderItem => orderItem.ProductId);
        builder.Property(orderItem => orderItem.ProductName);
        builder.Property(orderItem => orderItem.Quantity);

        builder.OwnsOne(orderItem => orderItem.Price, priceBuilder =>
        {
            priceBuilder.Property(money => money.Amount)
                        .HasColumnType("decimal(18, 2)");

            priceBuilder.Property(money => money.Currency)
                        .HasConversion(currency => currency.Code, code => Currency.FromStoredCode(code))
                        .HasDefaultValue(Currency.Bdt);

        });

        builder.Property(orderItem => orderItem.CreatedOnUtc);

        //builder.HasOne<Order>()
        //    .WithMany(order => order.OrderItems)
        //    .HasForeignKey(orderItem => orderItem.OrderId);


    }
}
