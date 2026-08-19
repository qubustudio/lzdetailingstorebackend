using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Catalog;

public sealed class Category : AuditableEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool IsActive { get; set; } = true;
    public int Orden { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
