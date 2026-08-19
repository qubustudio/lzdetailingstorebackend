using LzDetailingStore.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Content;

public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Direccion).HasMaxLength(300).IsRequired();
        builder.Property(b => b.Telefono).HasMaxLength(20);
        builder.Property(b => b.GoogleMapsUrl).HasMaxLength(500);
        builder.Property(b => b.Horario).HasMaxLength(200);
        builder.Property(b => b.ImageUrl).HasMaxLength(500);
    }
}
