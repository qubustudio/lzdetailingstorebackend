using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Identity;

public sealed class OAuthAccount : BaseEntity
{
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public string? Email { get; set; }

    public User User { get; set; } = null!;
}
