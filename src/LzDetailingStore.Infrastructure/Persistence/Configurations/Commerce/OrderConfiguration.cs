using LzDetailingStore.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Commerce;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.Status);

        // Optimistic concurrency using PostgreSQL's system column xmin.
        // xmin is a hidden system column that PostgreSQL updates automatically
        // on every row modification — ideal as a concurrency token.
        // IsConcurrencyToken + ValueGeneratedOnAddOrUpdate tells EF to read it
        // without trying to create it (it already exists on every PG table).
        // No migration needed: xmin is never created by EF, it is always there.
        builder.Property(o => o.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.Subtotal).HasPrecision(18, 2);
        builder.Property(o => o.ShippingCost).HasPrecision(18, 2);
        builder.Property(o => o.Total).HasPrecision(18, 2);

        builder.Property(o => o.MercadoPagoPreferenceId).HasMaxLength(100);
        builder.Property(o => o.MercadoPagoPaymentId).HasMaxLength(100);
        builder.Property(o => o.TrackingNumber).HasMaxLength(100);
        builder.Property(o => o.TrackingUrl).HasMaxLength(500);
        builder.Property(o => o.Notes).HasMaxLength(1000);

        builder.HasOne(o => o.ShippingAddress)
            .WithMany()
            .HasForeignKey(o => o.ShippingAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
