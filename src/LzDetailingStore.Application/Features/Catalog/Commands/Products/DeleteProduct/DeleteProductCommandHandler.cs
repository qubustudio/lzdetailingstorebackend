using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.DeleteProduct;

public sealed class DeleteProductCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await dbContext.Set<Product>()
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (product is null)
            return Result.NotFound("Producto no encontrado.");

        product.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
