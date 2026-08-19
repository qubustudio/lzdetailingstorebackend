using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Identity;

public sealed class Permission : BaseEntity
{
    public Guid ModuleId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public Module Module { get; set; } = null!;
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
