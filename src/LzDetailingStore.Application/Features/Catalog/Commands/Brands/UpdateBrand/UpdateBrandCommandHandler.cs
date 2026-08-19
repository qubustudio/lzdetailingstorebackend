using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Brands.UpdateBrand;

public sealed class UpdateBrandCommandHandler(
    DbContext dbContext,
    ISlugGenerator slugGenerator,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateBrandCommand, Result>
{
    public async Task<Result> Handle(UpdateBrandCommand request, CancellationToken ct)
    {
        var brand = await dbContext.Set<Brand>()
            .FirstOrDefaultAsync(b => b.Id == request.Id, ct);

        if (brand is null)
            return Result.NotFound("Marca no encontrada.");

        var duplicate = await dbContext.Set<Brand>()
            .AnyAsync(b => b.Id != request.Id
                && b.Nombre.ToLower() == request.Nombre.ToLower(), ct);

        if (duplicate)
            return Result.Conflict($"Ya existe otra marca con el nombre '{request.Nombre}'.");

        if (brand.Nombre != request.Nombre)
            brand.Slug = await slugGenerator.GenerateAsync(request.Nombre, "brands", ct);

        BrandCategory? categoria = null;
        if (!string.IsNullOrWhiteSpace(request.Categoria)
            && Enum.TryParse<BrandCategory>(request.Categoria, true, out var parsed))
        {
            categoria = parsed;
        }

        brand.Nombre = request.Nombre;
        brand.Descripcion = request.Descripcion;
        brand.Categoria = categoria;
        brand.Origen = request.Origen;
        brand.Especialidad = request.Especialidad;
        brand.Destacada = request.Destacada;
        brand.LogoUrl = request.LogoUrl;
        brand.ImagenSpotlight = request.ImagenSpotlight;

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
