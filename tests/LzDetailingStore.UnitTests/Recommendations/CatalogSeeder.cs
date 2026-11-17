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

    /// <summary>
    /// Seeds an active product with the given presentations (price, stock flag, active). An empty list seeds a
    /// product without variants. <paramref name="createdAt"/> lets tests control the default catalog order.
    /// </summary>
    public async Task<Product> ProductWithVariantsAsync(
        string name, DateTime? createdAt = null, params (decimal Precio, int Stock, bool Active)[] variants)
    {
        _sku++;
        var product = new Product
        {
            Sku = $"P-{_sku}",
            Name = name,
            Slug = $"producto-{_sku}",
            ImageUrl = "https://cdn/img.png",
            IsActive = true,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            // Stored value deliberately stale: public queries must compute the "desde" price themselves.
            PrecioBase = 1m,
            Brand = _brand,
            Category = _category,
            Variants = variants
                .Select((v, i) => new ProductVariant
                {
                    Sku = $"V-{_sku}-{i}",
                    Nombre = $"Presentación {i + 1}",
                    PrecioVenta = v.Precio,
                    SizeValue = 500,
                    SizeUnit = "ml",
                    Stock = v.Stock,
                    IsActive = v.Active,
                    SortOrder = i
                })
                .ToList()
        };

        ctx.Add(product);
        await ctx.SaveChangesAsync();
        return product;
    }

    /// <summary>
    /// Seeds the fixed measurement unit catalog. Rows are inserted out of order so tests prove the query sorts by
    /// <see cref="MeasurementUnit.Orden"/>.
    /// </summary>
    public void MeasurementUnits()
    {
        ctx.AddRange(
            new MeasurementUnit { Clave = "pza", Nombre = "Pieza", Orden = 5 },
            new MeasurementUnit { Clave = "ml", Nombre = "Mililitros", Orden = 1 },
            new MeasurementUnit { Clave = "kit", Nombre = "Kit", Orden = 6 },
            new MeasurementUnit { Clave = "L", Nombre = "Litros", Orden = 2 },
            new MeasurementUnit { Clave = "kg", Nombre = "Kilogramos", Orden = 4 },
            new MeasurementUnit { Clave = "g", Nombre = "Gramos", Orden = 3 });
        ctx.SaveChanges();
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
