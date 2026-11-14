namespace LzDetailingStore.Domain.Entities.Catalog;

/// <summary>
/// Fixed (non-administered) catalog of the units a product variant size can be expressed in.
/// Keyed by its short code (<see cref="Clave"/>, e.g. "ml", "L"), which <see cref="ProductVariant.SizeUnit"/>
/// references. Rows are seeded by migration, so it has no Guid id or audit fields.
/// </summary>
public sealed class MeasurementUnit
{
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
}
