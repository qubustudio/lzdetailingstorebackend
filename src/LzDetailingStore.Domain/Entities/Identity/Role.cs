using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Identity;

public sealed class Role : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool IsSystem { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
