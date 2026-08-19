namespace LzDetailingStore.Application.Features.Catalog.DTOs;

public sealed record CategoryDto(Guid Id, string Nombre, string Slug, string? Descripcion, int Orden);
