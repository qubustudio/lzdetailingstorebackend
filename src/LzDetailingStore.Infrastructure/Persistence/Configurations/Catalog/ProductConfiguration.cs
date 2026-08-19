using LzDetailingStore.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Sku).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(200).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.PrecioBase).HasPrecision(10, 2);
        builder.Property(p => p.PrecioOriginal).HasPrecision(10, 2);
        builder.Property(p => p.ImageUrl).HasMaxLength(500).IsRequired();
        builder.Property(p => p.ImageAlt).HasMaxLength(200);
        builder.Property(p => p.Rating).HasPrecision(2, 1);
        builder.Property(p => p.Badge).HasMaxLength(50);

        builder.HasOne(p => p.Brand).WithMany(b => b.Products).HasForeignKey(p => p.BrandId);
        builder.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId);
        builder.HasMany(p => p.Variants).WithOne(v => v.Product).HasForeignKey(v => v.ProductId);
        builder.HasMany(p => p.Images).WithOne().HasForeignKey(i => i.ProductId);
        builder.HasMany(p => p.Specs).WithOne().HasForeignKey(s => s.ProductId);
        builder.HasMany(p => p.ApplicationSteps).WithOne().HasForeignKey(a => a.ProductId);

        builder.HasIndex(p => p.BrandId);
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.IsActive);
    }
}
