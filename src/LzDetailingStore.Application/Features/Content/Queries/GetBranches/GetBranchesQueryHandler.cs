using LzDetailingStore.Application.Features.Content.DTOs;
using LzDetailingStore.Domain.Entities.Content;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Content.Queries.GetBranches;

public sealed class GetBranchesQueryHandler(DbContext dbContext)
    : IRequestHandler<GetBranchesQuery, Result<List<BranchDto>>>
{
    public async Task<Result<List<BranchDto>>> Handle(GetBranchesQuery request, CancellationToken ct)
    {
        var branches = await dbContext.Set<Branch>()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Nombre)
            .Select(b => new BranchDto(
                b.Id, b.Nombre, b.Direccion, b.Telefono,
                b.GoogleMapsUrl, b.Horario, b.ImageUrl))
            .ToListAsync(ct);

        return branches;
    }
}
