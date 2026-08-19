using LzDetailingStore.Application.Features.Identity.DTOs;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Identity;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Identity.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler(
    DbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtService jwtService) : IRequestHandler<RegisterUserCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(
        RegisterUserCommand request, CancellationToken ct)
    {
        var emailExists = await dbContext.Set<User>()
            .AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

        if (emailExists)
            return Result<AuthResponseDto>.Conflict("Email already exists");

        var user = new User
        {
            Nombre = request.Nombre,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(request.Password),
            Telefono = request.Telefono
        };

        var clienteRole = await dbContext.Set<Role>()
            .FirstOrDefaultAsync(r => r.Nombre == "Cliente", ct);

        if (clienteRole is not null)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = clienteRole.Id
            });
        }

        await dbContext.Set<User>().AddAsync(user, ct);
        await dbContext.SaveChangesAsync(ct);

        var roles = clienteRole is not null ? new List<string> { "Cliente" } : new List<string>();
        var (accessToken, expiresIn) = jwtService.GenerateToken(user, roles);

        var refreshToken = GenerateRefreshToken(user.Id);
        await dbContext.Set<Domain.Entities.Identity.RefreshToken>().AddAsync(refreshToken, ct);
        await dbContext.SaveChangesAsync(ct);

        return new AuthResponseDto(accessToken, refreshToken.Token, expiresIn);
    }

    private static Domain.Entities.Identity.RefreshToken GenerateRefreshToken(Guid userId)
    {
        var randomBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return new Domain.Entities.Identity.RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
    }
}
