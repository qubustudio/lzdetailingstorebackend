using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    DbContext dbContext,
    ISlugGenerator slugGenerator,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductCommand, Result>
{
    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await dbContext.Set<Product>()
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (product is null)
            return Result.NotFound("Producto no encontrado.");

        var brandExists = await dbContext.Set<Brand>()
            .AnyAsync(b => b.Id == request.BrandId && b.IsActive, ct);

        if (!brandExists)
            return Result.NotFound("Marca no encontrada.");

        var categoryExists = await dbContext.Set<Category>()
            .AnyAsync(c => c.Id == request.CategoryId && c.IsActive, ct);

        if (!categoryExists)
            return Result.NotFound("Categoría no encontrada.");

        if (product.Name != request.Name)
            product.Slug = await slugGenerator.GenerateAsync(request.Name, "products", ct);

        product.Name = request.Name;
        product.Description = request.Description;
        product.BrandId = request.BrandId;
        product.CategoryId = request.CategoryId;
        product.ImageUrl = request.ImageUrl;
        product.ImageAlt = request.ImageAlt;
        product.Badge = request.Badge;

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
