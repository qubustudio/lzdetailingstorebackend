using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Identity;

public sealed class Module : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Orden { get; set; }

    public ICollection<Permission> Permissions { get; set; } = [];
}
