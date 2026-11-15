using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.SetProductActive;

/// <summary>Activates or deactivates a product (inactive products are hidden from the storefront).</summary>
public sealed record SetProductActiveCommand(Guid ProductId, bool Activo) : IRequest<Result>;
