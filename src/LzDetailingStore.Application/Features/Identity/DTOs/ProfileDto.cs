namespace LzDetailingStore.Application.Features.Identity.DTOs;

public sealed record ProfileDto(
    Guid Id,
    string Nombre,
    string Email,
    string? Telefono,
    IList<string> Roles);
