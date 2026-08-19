using LzDetailingStore.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Identity;

public sealed class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Nombre).HasMaxLength(50).IsRequired();
        builder.Property(m => m.Slug).HasMaxLength(50).IsRequired();
        builder.HasIndex(m => m.Slug).IsUnique();
        builder.Property(m => m.Descripcion).HasMaxLength(200);
    }
}
