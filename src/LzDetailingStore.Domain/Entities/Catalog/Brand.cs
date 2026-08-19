using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;

namespace LzDetailingStore.Domain.Entities.Catalog;

public sealed class Brand : AuditableEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Descripcion { get; set; }
    public BrandCategory? Categoria { get; set; }
    public string? Badge { get; set; }
    public string? Origen { get; set; }
    public string? Especialidad { get; set; }
    public bool Destacada { get; set; }
    public string? HistoriaCompleta { get; set; }
    public string? ImagenSpotlight { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = [];
}
