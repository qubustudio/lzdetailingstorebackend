using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.DeleteProductImage;

public sealed class DeleteProductImageCommandHandler(
    DbContext dbContext,
    IS3Service s3Service,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductImageCommand, Result>
{
    public async Task<Result> Handle(DeleteProductImageCommand request, CancellationToken ct)
    {
        var image = await dbContext.Set<ProductImage>()
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

        if (image is null)
            return Result.NotFound("Imagen no encontrada.");

        // Extract key from URL for S3 deletion
        var uri = new Uri(image.ImageUrl);
        var key = uri.AbsolutePath.TrimStart('/');

        await s3Service.DeleteAsync(key, ct);

        dbContext.Set<ProductImage>().Remove(image);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
