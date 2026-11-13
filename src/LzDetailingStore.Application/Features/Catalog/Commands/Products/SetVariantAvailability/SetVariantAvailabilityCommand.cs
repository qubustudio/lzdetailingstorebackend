using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.SetVariantAvailability;

/// <summary>
/// Marks a product variant as available or sold out / on request. Only the availability flag
/// (stock) changes; prices and the rest of the variant data are untouched.
/// </summary>
public sealed record SetVariantAvailabilityCommand(
    Guid ProductId,
    Guid VariantId,
    bool Disponible) : IRequest<Result>;
