using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Brands.DeleteBrand;

public sealed class DeleteBrandCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteBrandCommand, Result>
{
    public async Task<Result> Handle(DeleteBrandCommand request, CancellationToken ct)
    {
        var brand = await dbContext.Set<Brand>()
            .FirstOrDefaultAsync(b => b.Id == request.Id, ct);

        if (brand is null)
            return Result.NotFound("Marca no encontrada.");

        brand.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
