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
public sealed record GetAdminProductsQuery(
    int Page = 1,
    int Limit = 20,
    string? Search = null,
    Guid? BrandId = null,
    Guid? CategoryId = null,
    bool? Activo = null,
    bool IncluyeCostos = false)
    : IRequest<Result<AdminProductPageDto>>
{
    public const int SearchMaxLength = 100;
}
