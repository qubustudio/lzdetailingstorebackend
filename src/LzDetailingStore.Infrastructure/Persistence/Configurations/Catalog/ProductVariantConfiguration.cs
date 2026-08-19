using LzDetailingStore.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Sku).HasMaxLength(50).IsRequired();
        builder.HasIndex(v => v.Sku).IsUnique();
        builder.Property(v => v.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(v => v.PrecioVenta).HasPrecision(10, 2).IsRequired();
        builder.Property(v => v.PrecioCosto).HasPrecision(10, 2).IsRequired();
        builder.Property(v => v.SizeValue).HasPrecision(8, 2).IsRequired();
        builder.Property(v => v.SizeUnit).HasMaxLength(20).IsRequired();
        builder.Property(v => v.Barcode).HasMaxLength(50);

        builder.HasIndex(v => new { v.ProductId, v.IsActive });
        builder.HasIndex(v => new { v.ProductId, v.SortOrder });
    }
}
