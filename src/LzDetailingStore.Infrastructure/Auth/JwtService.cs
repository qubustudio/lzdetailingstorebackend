using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LzDetailingStore.Infrastructure.Auth;

public sealed class JwtService(IConfiguration configuration) : IJwtService
{
    public (string AccessToken, int ExpiresIn) GenerateToken(User user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresInHours = int.Parse(configuration["Jwt:ExpiresInHours"] ?? "24");
        var expires = DateTime.UtcNow.AddHours(expiresInHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("nombre", user.Nombre)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresInHours * 3600);
    }
}
