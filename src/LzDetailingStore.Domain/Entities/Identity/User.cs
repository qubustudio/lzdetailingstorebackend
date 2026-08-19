using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Identity;

public sealed class User : AuditableEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? Telefono { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<OAuthAccount> OAuthAccounts { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
