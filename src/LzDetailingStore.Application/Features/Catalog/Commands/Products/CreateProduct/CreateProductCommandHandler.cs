using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.CreateProduct;

public sealed class CreateProductCommandHandler(
    DbContext dbContext,
    ISlugGenerator slugGenerator,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var brandExists = await dbContext.Set<Brand>()
            .AnyAsync(b => b.Id == request.BrandId && b.IsActive, ct);

        if (!brandExists)
            return Result<Guid>.NotFound("Marca no encontrada.");

        var categoryExists = await dbContext.Set<Category>()
            .AnyAsync(c => c.Id == request.CategoryId && c.IsActive, ct);

        if (!categoryExists)
            return Result<Guid>.NotFound("Categoría no encontrada.");

        var slug = await slugGenerator.GenerateAsync(request.Name, "products", ct);

        var product = new Product
        {
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            BrandId = request.BrandId,
            CategoryId = request.CategoryId,
            ImageUrl = request.ImageUrl,
            ImageAlt = request.ImageAlt,
            Badge = request.Badge,
            IsActive = true
        };

        await dbContext.Set<Product>().AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(product.Id);
    }
}
