namespace LzDetailingStore.Application.Features.Content.DTOs;

public sealed record BranchDto(
    Guid Id,
    string Nombre,
    string Direccion,
    string? Telefono,
    string? GoogleMapsUrl,
    string? Horario,
    string? ImageUrl);
