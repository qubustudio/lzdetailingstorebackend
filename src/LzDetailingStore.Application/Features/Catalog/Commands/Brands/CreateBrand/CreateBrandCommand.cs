using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Brands.CreateBrand;

public sealed record CreateBrandCommand(
    string Nombre,
    string? Descripcion,
    string? Categoria,
    string? Origen,
    string? Especialidad,
    bool Destacada,
    string? LogoUrl,
    string? ImagenSpotlight) : IRequest<Result<Guid>>;
