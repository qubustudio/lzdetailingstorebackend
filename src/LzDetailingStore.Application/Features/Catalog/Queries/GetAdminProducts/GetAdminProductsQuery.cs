using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetAdminProducts;

/// <summary>Admin product list: inactive products are included unless <see cref="Activo"/> filters them.</summary>
/// <param name="Search">Case-insensitive match on product name, product SKU or any variant SKU.</param>
/// <param name="Activo">Optional filter on the product active flag; null lists both.</param>
/// <param name="IncluyeCostos">
/// Whether the caller holds <see cref="AdminProductPermissions.Costos"/>. Resolved by the API layer; defaults to
/// false so cost visibility is only reported when explicitly allowed.
/// </param>
/// <param name="Agotados">
/// Sold-out filter (case-insensitive): <see cref="AgotadosCompleto"/> keeps products with no available active variant
/// (the same rule as the dashboard "Productos agotados" tile); <see cref="AgotadosAlguno"/> keeps products with at
/// least one active sold-out variant (the same rule as VariantesAgotadas). Null or empty applies no filter.
/// </param>
public sealed record GetAdminProductsQuery(
    int Page = 1,
    int Limit = 20,
    string? Search = null,
    Guid? BrandId = null,
    Guid? CategoryId = null,
    bool? Activo = null,
    bool IncluyeCostos = false,
    string? Agotados = null)
    : IRequest<Result<AdminProductPageDto>>
{
    public const int SearchMaxLength = 100;
    public const string AgotadosCompleto = "completo";
    public const string AgotadosAlguno = "alguno";
}
