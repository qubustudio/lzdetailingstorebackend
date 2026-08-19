namespace LzDetailingStore.Application.Features.Catalog.DTOs;

public sealed record ProductImageDto(Guid Id, string ImageUrl, string? ImageAlt, int Orden);
