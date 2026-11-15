using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetAdminProductById;

/// <summary>Any product by id for the admin panel, inactive products and variants included.</summary>
/// <param name="IncluyeCostos">
/// Whether the caller holds <see cref="AdminProductPermissions.Costos"/>. Resolved by the API layer; defaults to
/// false so variant costs are only returned when explicitly allowed.
/// </param>
public sealed record GetAdminProductByIdQuery(Guid Id, bool IncluyeCostos = false)
    : IRequest<Result<AdminProductDetailDto>>;
