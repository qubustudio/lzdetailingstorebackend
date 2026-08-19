using LzDetailingStore.Domain.Entities.Content;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Content.Commands.Branches.DeleteBranch;

public sealed class DeleteBranchCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteBranchCommand, Result>
{
    public async Task<Result> Handle(DeleteBranchCommand request, CancellationToken ct)
    {
        var branch = await dbContext.Set<Branch>()
            .FirstOrDefaultAsync(b => b.Id == request.Id, ct);

        if (branch is null)
            return Result.NotFound("Sucursal no encontrada.");

        branch.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
