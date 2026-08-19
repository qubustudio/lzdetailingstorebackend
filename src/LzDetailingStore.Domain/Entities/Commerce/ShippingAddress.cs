using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Commerce;

public sealed class ShippingAddress : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Calle { get; set; } = string.Empty;
    public string NumeroExterior { get; set; } = string.Empty;
    public string? NumeroInterior { get; set; }
    public string Colonia { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Referencia { get; set; }
    public bool EsPrincipal { get; set; }
    public bool IsActive { get; set; } = true;
}
