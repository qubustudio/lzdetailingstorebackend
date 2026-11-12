using System.Linq.Expressions;
using LzDetailingStore.Domain.Entities.Catalog;

namespace LzDetailingStore.Application.Features.Recommendations;

/// <summary>
/// Shared definition of a product that can be shown as a recommendation:
/// active, with at least one active variant in stock. Stock quantities never leave this predicate.
/// </summary>
public static class ProductRelationVisibility
{
    public const int MinVisibleApprovedPerType = 2;
    public const int MaxPerTypePerRequest = 3;

    public static readonly Expression<Func<Product, bool>> IsVisible =
        p => p.IsActive && p.Variants.Any(v => v.IsActive && v.Stock > 0);
}
