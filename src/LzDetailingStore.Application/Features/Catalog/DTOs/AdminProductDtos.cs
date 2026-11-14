namespace LzDetailingStore.Application.Features.Catalog.DTOs;

/// <summary>Permissions that shape the admin product endpoints and responses.</summary>
public static class AdminProductPermissions
{
    /// <summary>Read access to the admin product list and detail.</summary>
    public const string Ver = "productos.ver";

    /// <summary>Edit product data and activate / deactivate products.</summary>
    public const string Editar = "productos.editar";

    /// <summary>
    /// Unlocks the variant cost price in the admin product detail. Granted by the edit permission (no dedicated
    /// slug): without it <c>precioCosto</c> is null and <c>incluyeCostos</c> is false (Operativo).
    /// </summary>
    public const string Costos = Editar;
}

/// <summary>
/// Paged admin product list. Same paging fields as <c>PagedResult</c>, plus <see cref="IncluyeCostos"/>
/// (the list itself carries no cost fields; the flag tells the client what the detail will show).
/// </summary>
public sealed record AdminProductPageDto(
    bool IncluyeCostos,
    IReadOnlyList<AdminProductListItemDto> Data,
    int Total,
    int Page,
    int TotalPages);

/// <param name="TotalVariantes">Active variants of the product.</param>
/// <param name="VariantesAgotadas">Active variants marked as sold out / on request.</param>
public sealed record AdminProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string Sku,
    string ImageUrl,
    string BrandName,
    string CategoryName,
    bool IsActive,
    decimal? PrecioBase,
    int TotalVariantes,
    int VariantesAgotadas);

/// <summary>
/// Admin product detail, inactive variants included. When <see cref="IncluyeCostos"/> is false every
/// <c>PrecioCosto</c> is null.
/// </summary>
public sealed record AdminProductDetailDto(
    bool IncluyeCostos,
    Guid Id,
    string Name,
    string Slug,
    string Sku,
    string? Description,
    Guid BrandId,
    string BrandName,
    Guid CategoryId,
    string CategoryName,
    string ImageUrl,
    string? ImageAlt,
    string? Badge,
    bool IsActive,
    decimal? PrecioBase,
    decimal? PrecioOriginal,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<AdminProductVariantDto> Variants);

/// <summary>
/// Admin view of a variant. Stock is an availability flag and is only exposed as <see cref="Disponible"/>.
/// </summary>
public sealed record AdminProductVariantDto(
    Guid Id,
    string Sku,
    string Nombre,
    decimal PrecioVenta,
    decimal? PrecioCosto,
    decimal SizeValue,
    string SizeUnit,
    string? Barcode,
    bool Disponible,
    bool IsActive,
    int SortOrder);
