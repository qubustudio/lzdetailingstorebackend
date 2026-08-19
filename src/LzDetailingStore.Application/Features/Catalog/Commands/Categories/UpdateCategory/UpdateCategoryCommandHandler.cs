using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Categories.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    DbContext dbContext,
    ISlugGenerator slugGenerator,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCategoryCommand, Result>
{
    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await dbContext.Set<Category>()
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (category is null)
            return Result.NotFound("Categoría no encontrada.");

        var duplicate = await dbContext.Set<Category>()
            .AnyAsync(c => c.Id != request.Id
                && c.Nombre.ToLower() == request.Nombre.ToLower(), ct);

        if (duplicate)
            return Result.Conflict($"Ya existe otra categoría con el nombre '{request.Nombre}'.");

        if (category.Nombre != request.Nombre)
            category.Slug = await slugGenerator.GenerateAsync(request.Nombre, "categories", ct);

        category.Nombre = request.Nombre;
        category.Descripcion = request.Descripcion;
        category.Orden = request.Orden;

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
