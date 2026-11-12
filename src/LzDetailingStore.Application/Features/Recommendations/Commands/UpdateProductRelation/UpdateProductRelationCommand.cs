using LzDetailingStore.Application.Features.Recommendations.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.UpdateProductRelation;

/// <param name="Status">"aprobado", "rechazado" or "borrador" (optional).</param>
/// <param name="Reason">New customer-facing reason (optional; empty string clears it).</param>
/// <param name="ReviewerId">Set by the endpoint from the authenticated user.</param>
public sealed record UpdateProductRelationCommand(
    Guid Id,
    string? Status = null,
    string? Reason = null,
    Guid? ReviewerId = null) : IRequest<Result<ProductRelationAdminDto>>;
