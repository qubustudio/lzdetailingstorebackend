using LzDetailingStore.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Commerce;

public sealed class ShippingAddressConfiguration : IEntityTypeConfiguration<ShippingAddress>
{
    public void Configure(EntityTypeBuilder<ShippingAddress> builder)
    {
        builder.HasKey(sa => sa.Id);
        builder.HasIndex(sa => sa.UserId);

        builder.Property(sa => sa.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(sa => sa.Calle).HasMaxLength(200).IsRequired();
        builder.Property(sa => sa.NumeroExterior).HasMaxLength(20).IsRequired();
        builder.Property(sa => sa.NumeroInterior).HasMaxLength(20);
        builder.Property(sa => sa.Colonia).HasMaxLength(100).IsRequired();
        builder.Property(sa => sa.Ciudad).HasMaxLength(100).IsRequired();
        builder.Property(sa => sa.Estado).HasMaxLength(50).IsRequired();
        builder.Property(sa => sa.CodigoPostal).HasMaxLength(10).IsRequired();
        builder.Property(sa => sa.Telefono).HasMaxLength(20);
        builder.Property(sa => sa.Referencia).HasMaxLength(300);
    }
}
