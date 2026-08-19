using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.ManageProductSteps;

public sealed class ManageProductStepsCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ManageProductStepsCommand, Result>
{
    public async Task<Result> Handle(ManageProductStepsCommand request, CancellationToken ct)
    {
        var productExists = await dbContext.Set<Product>()
            .AnyAsync(p => p.Id == request.ProductId, ct);

        if (!productExists)
            return Result.NotFound("Producto no encontrado.");

        // Remove all existing steps for this product
        var existingSteps = await dbContext.Set<ProductApplicationStep>()
            .Where(s => s.ProductId == request.ProductId)
            .ToListAsync(ct);

        dbContext.Set<ProductApplicationStep>().RemoveRange(existingSteps);

        // Add new steps
        var newSteps = request.Steps.Select(s => new ProductApplicationStep
        {
            ProductId = request.ProductId,
            StepNumber = s.StepNumber,
            Title = s.Title,
            Description = s.Description
        });

        await dbContext.Set<ProductApplicationStep>().AddRangeAsync(newSteps, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
