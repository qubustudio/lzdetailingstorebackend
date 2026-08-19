namespace LzDetailingStore.Application.Features.Catalog.DTOs;

public sealed record BrandDto(
    Guid Id,
    string Nombre,
    string Slug,
    string? LogoUrl,
    string? Descripcion,
    string? Categoria,
    string? Origen,
    string? Especialidad,
    bool Destacada,
    string? ImagenSpotlight);
