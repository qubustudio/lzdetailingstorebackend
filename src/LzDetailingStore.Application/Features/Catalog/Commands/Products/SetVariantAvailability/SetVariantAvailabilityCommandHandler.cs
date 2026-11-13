using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.SetVariantAvailability;

public sealed class SetVariantAvailabilityCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetVariantAvailabilityCommand, Result>
{
    public async Task<Result> Handle(SetVariantAvailabilityCommand request, CancellationToken ct)
    {
        var variant = await dbContext.Set<ProductVariant>()
            .FirstOrDefaultAsync(v => v.Id == request.VariantId && v.ProductId == request.ProductId, ct);

        if (variant is null)
            return Result.NotFound("Variante no encontrada.");

        variant.SetAvailability(request.Disponible);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
