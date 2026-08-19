using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.UploadProductImage;

public sealed class UploadProductImageCommandHandler(
    DbContext dbContext,
    IS3Service s3Service,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadProductImageCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UploadProductImageCommand request, CancellationToken ct)
    {
        var productExists = await dbContext.Set<Product>()
            .AnyAsync(p => p.Id == request.ProductId, ct);

        if (!productExists)
            return Result<Guid>.NotFound("Producto no encontrado.");

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        var key = $"products/{request.ProductId}/{Guid.NewGuid()}{extension}";

        var imageUrl = await s3Service.UploadAsync(
            request.FileStream, key, request.ContentType, ct);

        var image = new ProductImage
        {
            ProductId = request.ProductId,
            ImageUrl = imageUrl,
            ImageAlt = request.ImageAlt,
            Orden = request.Orden
        };

        await dbContext.Set<ProductImage>().AddAsync(image, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(image.Id);
    }
}
