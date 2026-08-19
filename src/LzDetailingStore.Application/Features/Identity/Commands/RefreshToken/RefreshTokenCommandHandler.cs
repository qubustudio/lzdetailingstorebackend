using LzDetailingStore.Application.Features.Identity.DTOs;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Identity;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Identity.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    DbContext dbContext,
    IJwtService jwtService) : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var existingToken = await dbContext.Set<Domain.Entities.Identity.RefreshToken>()
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == request.Token, ct);

        if (existingToken is null)
            return Result<AuthResponseDto>.Failure(new Error("INVALID_TOKEN", "Invalid refresh token"), 401);

        // If token is revoked, invalidate entire family (security breach detection)
        if (existingToken.IsRevoked)
        {
            await RevokeAllUserTokensAsync(existingToken.UserId, ct);
            return Result<AuthResponseDto>.Failure(new Error("TOKEN_REVOKED", "Token has been revoked. Please re-authenticate."), 401);
        }

        if (existingToken.IsExpired)
            return Result<AuthResponseDto>.Failure(new Error("TOKEN_EXPIRED", "Refresh token expired"), 401);

        // Revoke old token
        existingToken.RevokedAt = DateTime.UtcNow;

        // Generate new tokens
        var user = existingToken.User;
        var roles = user.UserRoles.Select(ur => ur.Role.Nombre).ToList();
        var (accessToken, expiresIn) = jwtService.GenerateToken(user, roles);

        var newRefreshToken = GenerateRefreshToken(user.Id);
        existingToken.ReplacedByToken = newRefreshToken.Token;

        await dbContext.Set<Domain.Entities.Identity.RefreshToken>().AddAsync(newRefreshToken, ct);
        await dbContext.SaveChangesAsync(ct);

        return new AuthResponseDto(accessToken, newRefreshToken.Token, expiresIn);
    }

    private async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct)
    {
        var activeTokens = await dbContext.Set<Domain.Entities.Identity.RefreshToken>()
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
            token.RevokedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);
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
