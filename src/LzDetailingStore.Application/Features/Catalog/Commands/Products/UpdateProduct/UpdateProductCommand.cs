using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    Guid BrandId,
    Guid CategoryId,
    string ImageUrl,
    string? ImageAlt,
    string? Badge) : IRequest<Result>;
