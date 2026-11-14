using LzDetailingStore.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Catalog;

public sealed class MeasurementUnitConfiguration : IEntityTypeConfiguration<MeasurementUnit>
{
    public void Configure(EntityTypeBuilder<MeasurementUnit> builder)
    {
        builder.ToTable("unidad_medida");
        builder.HasKey(u => u.Clave);
        builder.Property(u => u.Clave).HasMaxLength(20);
        builder.Property(u => u.Nombre).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Orden).IsRequired();
    }
}
