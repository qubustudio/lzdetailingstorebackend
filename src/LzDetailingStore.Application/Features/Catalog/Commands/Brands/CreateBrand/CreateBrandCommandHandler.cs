using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Brands.CreateBrand;

public sealed class CreateBrandCommandHandler(
    DbContext dbContext,
    ISlugGenerator slugGenerator,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateBrandCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBrandCommand request, CancellationToken ct)
    {
        var exists = await dbContext.Set<Brand>()
            .AnyAsync(b => b.Nombre.ToLower() == request.Nombre.ToLower(), ct);

        if (exists)
            return Result<Guid>.Conflict($"Ya existe una marca con el nombre '{request.Nombre}'.");

        var slug = await slugGenerator.GenerateAsync(request.Nombre, "brands", ct);

        BrandCategory? categoria = null;
        if (!string.IsNullOrWhiteSpace(request.Categoria)
            && Enum.TryParse<BrandCategory>(request.Categoria, true, out var parsed))
        {
            categoria = parsed;
        }

        var brand = new Brand
        {
            Nombre = request.Nombre,
            Slug = slug,
            Descripcion = request.Descripcion,
            Categoria = categoria,
            Origen = request.Origen,
            Especialidad = request.Especialidad,
            Destacada = request.Destacada,
            LogoUrl = request.LogoUrl,
            ImagenSpotlight = request.ImagenSpotlight,
            IsActive = true
        };

        await dbContext.Set<Brand>().AddAsync(brand, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(brand.Id);
    }
}
