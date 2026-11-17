using System.Linq.Expressions;
using LzDetailingStore.Domain.Entities.Catalog;

namespace LzDetailingStore.Application.Features.Catalog;

/// <summary>
/// Shared rules of the public (store) catalog.
/// A variant is visible when it is active and available when it is also in stock.
/// A product is listed when it is active and has at least one visible variant, so the store never
/// shows a product without presentations. Stock quantities never leave these rules.
/// </summary>
public static class PublicCatalog
{
    public static readonly Expression<Func<Product, bool>> IsListed =
        p => p.IsActive && p.Variants.Any(v => v.IsActive);

    /// <summary>
    /// Projects listed products with their availability and computed "desde" price, so filters,
    /// sorting and paging all run in the database on the same value the customer sees.
    /// </summary>
    public static IQueryable<ListedProduct> SelectListed(this IQueryable<Product> query) =>
        query.Select(p => new ListedProduct
        {
            Product = p,
            HasAvailableVariant = p.Variants.Any(v => v.IsActive && v.Stock > 0),
            PrecioDesde = p.Variants.Where(v => v.IsActive && v.Stock > 0).Min(v => (decimal?)v.PrecioVenta)
                          ?? p.Variants.Where(v => v.IsActive).Min(v => (decimal?)v.PrecioVenta)
        });

    /// <summary>
    /// "Desde" price shown to customers: the cheapest available presentation, or the cheapest visible one
    /// when the product is fully sold out. Never changes the stored <see cref="Product.PrecioBase"/>.
    /// </summary>
    public static decimal? PrecioDesde(IEnumerable<ProductVariant> variants)
    {
        var visible = variants.Where(v => v.IsActive).ToList();
        var available = visible.Where(v => v.Stock > 0).ToList();
        var pool = available.Count != 0 ? available : visible;
        return pool.Count != 0 ? pool.Min(v => v.PrecioVenta) : null;
    }
}

/// <summary>Query shape of a listed product. Only used inside database queries.</summary>
public sealed class ListedProduct
{
    public Product Product { get; init; } = null!;
    public bool HasAvailableVariant { get; init; }
    public decimal? PrecioDesde { get; init; }
}
