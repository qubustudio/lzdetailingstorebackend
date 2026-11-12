using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.DeleteProductRelation;

public sealed class DeleteProductRelationCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductRelationCommand, Result>
{
    public async Task<Result> Handle(DeleteProductRelationCommand request, CancellationToken ct)
    {
        var relation = await dbContext.Set<ProductRelation>()
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct);

        if (relation is null)
            return Result.NotFound("Recomendación no encontrada.");

        dbContext.Set<ProductRelation>().Remove(relation);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
