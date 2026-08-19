using LzDetailingStore.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(ci => ci.Id);
        builder.HasIndex(ci => new { ci.CartId, ci.VariantId }).IsUnique();

        builder.HasOne(ci => ci.Variant)
            .WithMany()
            .HasForeignKey(ci => ci.VariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
