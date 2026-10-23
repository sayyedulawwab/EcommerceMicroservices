using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

internal sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("StockReservations");

        builder.HasKey(reservation => reservation.Id).IsClustered(false);

        builder.Property(reservation => reservation.Id)
               .HasConversion(reservationId => reservationId.Value, value => new StockReservationId(value));

        builder.Property<long>("ClusterId").UseIdentityColumn();

        builder.HasIndex("ClusterId").IsClustered();

        builder.Property(reservation => reservation.OrderId);
        builder.Property(reservation => reservation.ProductId)
               .HasConversion(productId => productId.Value, value => new ProductId(value));
        builder.Property(reservation => reservation.Quantity);
        builder.Property(reservation => reservation.Status);
        builder.Property(reservation => reservation.UpdatedOnUtc);
        builder.Property(reservation => reservation.CreatedOnUtc);

        builder.HasIndex(reservation => new { reservation.OrderId, reservation.ProductId })
               .IsUnique();
    }
}
