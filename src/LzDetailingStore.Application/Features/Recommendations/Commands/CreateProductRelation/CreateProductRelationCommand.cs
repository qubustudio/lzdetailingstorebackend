using LzDetailingStore.Application.Features.Recommendations.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.CreateProductRelation;

/// <param name="Tipo">"complementario" or "similar".</param>
/// <param name="ReviewerId">Set by the endpoint from the authenticated user.</param>
public sealed record CreateProductRelationCommand(
    Guid ProductId,
    Guid RelatedProductId,
    string Tipo,
    string? Reason,
    Guid? ReviewerId = null) : IRequest<Result<ProductRelationAdminDto>>;
