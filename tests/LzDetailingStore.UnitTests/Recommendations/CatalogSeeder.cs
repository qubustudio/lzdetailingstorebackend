using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel.Enums;
using LzDetailingStore.SharedKernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.UnitTests.Recommendations;

/// <summary>Test data helpers for the catalog/recommendation tests.</summary>
public sealed class CatalogSeeder(CatalogTestContext ctx)
{
    private readonly Brand _brand = new() { Nombre = "Marca Uno", Slug = "marca-uno" };
    private readonly Category _category = new() { Nombre = "Lavado", Slug = "lavado" };
    private int _sku;

    public async Task<Product> ProductAsync(string name, bool active = true, int stock = 10, bool variantActive = true)
    {
        _sku++;
        var product = new Product
        {
            Sku = $"P-{_sku}",
            Name = name,
            Slug = $"producto-{_sku}",
            Description = $"Descripción de {name}",
            ImageUrl = "https://cdn/img.png",
            IsActive = active,
            Brand = _brand,
            Category = _category,
            Variants =
            [
                new ProductVariant
                {
                    Sku = $"V-{_sku}",
                    Nombre = "500 ml",
                    PrecioVenta = 199m,
                    PrecioCosto = 87.35m,
                    SizeValue = 500,
                    SizeUnit = "ml",
                    Stock = stock,
                    IsActive = variantActive
                }
            ]
        };

        ctx.Add(product);
        await ctx.SaveChangesAsync();
        return product;
    }

    public async Task<ProductRelation> RelationAsync(
        Product from, Product to, ProductRelationType type,
        ProductRelationStatus status = ProductRelationStatus.Approved,
        int sortOrder = 0, string? reason = null)
    {
        var relation = new ProductRelation
        {
            ProductId = from.Id,
            RelatedProductId = to.Id,
            Type = type,
            Status = status,
            Source = ProductRelationSource.Manual,
            SortOrder = sortOrder,
            Reason = reason
        };

        ctx.Add(relation);
        await ctx.SaveChangesAsync();
        return relation;
    }
}

public sealed class PassthroughUnitOfWork(DbContext ctx) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => ctx.SaveChangesAsync(ct);
}
