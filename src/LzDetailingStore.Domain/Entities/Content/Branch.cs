using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Content;

public sealed class Branch : AuditableEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? GoogleMapsUrl { get; set; }
    public string? Horario { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
