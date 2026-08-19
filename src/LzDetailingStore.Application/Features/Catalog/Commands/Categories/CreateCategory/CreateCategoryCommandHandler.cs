using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Categories.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    DbContext dbContext,
    ISlugGenerator slugGenerator,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var exists = await dbContext.Set<Category>()
            .AnyAsync(c => c.Nombre.ToLower() == request.Nombre.ToLower(), ct);

        if (exists)
            return Result<Guid>.Conflict($"Ya existe una categoría con el nombre '{request.Nombre}'.");

        var slug = await slugGenerator.GenerateAsync(request.Nombre, "categories", ct);

        var category = new Category
        {
            Nombre = request.Nombre,
            Slug = slug,
            Descripcion = request.Descripcion,
            Orden = request.Orden,
            IsActive = true
        };

        await dbContext.Set<Category>().AddAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(category.Id);
    }
}
