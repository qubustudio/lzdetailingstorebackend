using LzDetailingStore.Domain.Entities.Content;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Content.Commands.Branches.UpdateBranch;

public sealed class UpdateBranchCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateBranchCommand, Result>
{
    public async Task<Result> Handle(UpdateBranchCommand request, CancellationToken ct)
    {
        var branch = await dbContext.Set<Branch>()
            .FirstOrDefaultAsync(b => b.Id == request.Id, ct);

        if (branch is null)
            return Result.NotFound("Sucursal no encontrada.");

        branch.Nombre = request.Nombre;
        branch.Direccion = request.Direccion;
        branch.Telefono = request.Telefono;
        branch.GoogleMapsUrl = request.GoogleMapsUrl;
        branch.Horario = request.Horario;
        branch.ImageUrl = request.ImageUrl;

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
