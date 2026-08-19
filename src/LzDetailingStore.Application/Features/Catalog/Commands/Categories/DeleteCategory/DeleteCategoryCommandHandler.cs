using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Categories.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await dbContext.Set<Category>()
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (category is null)
            return Result.NotFound("Categoría no encontrada.");

        category.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
