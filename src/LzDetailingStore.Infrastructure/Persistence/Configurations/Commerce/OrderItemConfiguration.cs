using LzDetailingStore.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Commerce;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
        builder.Property(oi => oi.Subtotal).HasPrecision(18, 2);

        builder.Property(oi => oi.ProductName).HasMaxLength(200);
        builder.Property(oi => oi.VariantName).HasMaxLength(150);
        builder.Property(oi => oi.Sku).HasMaxLength(50);
        builder.Property(oi => oi.ImageUrl).HasMaxLength(500);
    }
}
