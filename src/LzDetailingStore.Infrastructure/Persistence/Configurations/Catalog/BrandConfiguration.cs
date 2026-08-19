using LzDetailingStore.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Catalog;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(b => b.Slug).IsUnique();
        builder.Property(b => b.LogoUrl).HasMaxLength(500);
        builder.Property(b => b.Descripcion).HasMaxLength(1000);
        builder.Property(b => b.Badge).HasMaxLength(50);
        builder.Property(b => b.Origen).HasMaxLength(100);
        builder.Property(b => b.Especialidad).HasMaxLength(200);
        builder.Property(b => b.ImagenSpotlight).HasMaxLength(500);
        builder.Property(b => b.Categoria).HasConversion<string>().HasMaxLength(50);
    }
}
