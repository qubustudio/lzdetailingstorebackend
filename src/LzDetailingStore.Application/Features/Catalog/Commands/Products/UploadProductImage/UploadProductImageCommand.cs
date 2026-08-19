using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.UploadProductImage;

public sealed record UploadProductImageCommand(
    Guid ProductId,
    Stream FileStream,
    string FileName,
    string ContentType,
    string? ImageAlt,
    int Orden) : IRequest<Result<Guid>>;
