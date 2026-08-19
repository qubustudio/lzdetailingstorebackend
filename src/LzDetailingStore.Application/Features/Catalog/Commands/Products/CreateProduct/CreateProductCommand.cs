using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string? Description,
    Guid BrandId,
    Guid CategoryId,
    string ImageUrl,
    string? ImageAlt,
    string? Badge) : IRequest<Result<Guid>>;
