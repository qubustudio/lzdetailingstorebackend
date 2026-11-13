using LzDetailingStore.Application.Features.Identity.DTOs;
using LzDetailingStore.Domain.Entities.Identity;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Identity.Queries.GetProfile;

public sealed class GetProfileQueryHandler(
    DbContext dbContext) : IRequestHandler<GetProfileQuery, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(GetProfileQuery request, CancellationToken ct)
    {
        // Single projected query: roles plus every permission slug granted through those roles.
        var user = await dbContext.Set<User>()
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new
            {
                u.Id,
                u.Nombre,
                u.Email,
                u.Telefono,
                Roles = u.UserRoles.Select(ur => ur.Role.Nombre).ToList(),
                PermissionSlugs = u.UserRoles
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.Slug)
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return Result<ProfileDto>.NotFound("User not found");

        // Dedupe + sort in memory: the slug list is tiny and this avoids provider-specific translation.
        var permisos = user.PermissionSlugs
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        return new ProfileDto(
            user.Id,
            user.Nombre,
            user.Email,
            user.Telefono,
            user.Roles,
            permisos);
    }
}
