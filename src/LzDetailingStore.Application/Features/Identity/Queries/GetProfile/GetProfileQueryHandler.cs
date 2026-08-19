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
        var user = await dbContext.Set<User>()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user is null)
            return Result<ProfileDto>.NotFound("User not found");

        var roles = user.UserRoles.Select(ur => ur.Role.Nombre).ToList();

        return new ProfileDto(
            user.Id,
            user.Nombre,
            user.Email,
            user.Telefono,
            roles);
    }
}
