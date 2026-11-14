using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.SetProductActive;

public sealed class SetProductActiveCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetProductActiveCommand, Result>
{
    public async Task<Result> Handle(SetProductActiveCommand request, CancellationToken ct)
    {
        var product = await dbContext.Set<Product>()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);

        if (product is null)
            return Result.NotFound("Producto no encontrado.");

        product.IsActive = request.Activo;
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
