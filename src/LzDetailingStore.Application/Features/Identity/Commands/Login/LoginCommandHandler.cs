using LzDetailingStore.Application.Features.Identity.DTOs;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Identity;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Identity.Commands.Login;

public sealed class LoginCommandHandler(
    DbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtService jwtService) : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await dbContext.Set<User>()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

        if (user is null || user.PasswordHash is null)
            return Result<AuthResponseDto>.Failure(new Error("INVALID_CREDENTIALS", "Invalid credentials"), 401);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponseDto>.Failure(new Error("INVALID_CREDENTIALS", "Invalid credentials"), 401);

        if (!user.IsActive)
            return Result<AuthResponseDto>.Failure(new Error("ACCOUNT_DISABLED", "Account is disabled"), 403);

        var roles = user.UserRoles.Select(ur => ur.Role.Nombre).ToList();
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
