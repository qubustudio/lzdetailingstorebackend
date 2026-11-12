using LzDetailingStore.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.UnitTests.Recommendations;

/// <summary>
/// Minimal in-memory DbContext for the product-relation handlers. Registers only the catalog
/// entities they touch and ignores navigations (images, specs, steps) that are not needed.
/// </summary>
public sealed class CatalogTestContext(DbContextOptions<CatalogTestContext> options) : DbContext(options)
{
    public static CatalogTestContext Create()
    {
        var options = new DbContextOptionsBuilder<CatalogTestContext>()
            .UseInMemoryDatabase($"catalog-tests-{Guid.NewGuid()}")
            .Options;
        return new CatalogTestContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>(b => b.HasKey(x => x.Id));
        modelBuilder.Entity<Category>(b => b.HasKey(x => x.Id));

        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.Ignore(p => p.Images);
            b.Ignore(p => p.Specs);
            b.Ignore(p => p.ApplicationSteps);
            b.HasOne(p => p.Brand).WithMany(x => x.Products).HasForeignKey(p => p.BrandId);
            b.HasOne(p => p.Category).WithMany(x => x.Products).HasForeignKey(p => p.CategoryId);
            b.HasMany(p => p.Variants).WithOne(v => v.Product).HasForeignKey(v => v.ProductId);
        });

        modelBuilder.Entity<ProductVariant>(b => b.HasKey(v => v.Id));

        modelBuilder.Entity<ProductRelation>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductId);
            b.HasOne(r => r.RelatedProduct).WithMany().HasForeignKey(r => r.RelatedProductId);
        });
    }
}
