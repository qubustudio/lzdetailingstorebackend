using LzDetailingStore.Domain.Entities.Content;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Content.Commands.Branches.CreateBranch;

public sealed class CreateBranchCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateBranchCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBranchCommand request, CancellationToken ct)
    {
        var branch = new Branch
        {
            Nombre = request.Nombre,
            Direccion = request.Direccion,
            Telefono = request.Telefono,
            GoogleMapsUrl = request.GoogleMapsUrl,
            Horario = request.Horario,
            ImageUrl = request.ImageUrl,
            IsActive = true
        };

        await dbContext.Set<Branch>().AddAsync(branch, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(branch.Id);
    }
}
