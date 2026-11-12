using LzDetailingStore.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductRelationConfiguration : IEntityTypeConfiguration<ProductRelation>
{
    public void Configure(EntityTypeBuilder<ProductRelation> builder)
    {
        builder.ToTable("product_relation", t =>
            t.HasCheckConstraint(
                "ck_product_relation_not_self",
                "product_id <> related_product_id"));

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.Source).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(ProductRelation.ReasonMaxLength);

        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.RelatedProduct)
            .WithMany()
            .HasForeignKey(r => r.RelatedProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.ProductId, r.RelatedProductId, r.Type }).IsUnique();
        builder.HasIndex(r => new { r.ProductId, r.Status, r.Type, r.SortOrder });
        builder.HasIndex(r => r.RelatedProductId);
    }
}
