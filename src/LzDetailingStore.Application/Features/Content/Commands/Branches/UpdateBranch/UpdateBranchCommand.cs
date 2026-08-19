using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Content.Commands.Branches.UpdateBranch;

public sealed record UpdateBranchCommand(
    Guid Id,
    string Nombre,
    string Direccion,
    string? Telefono,
    string? GoogleMapsUrl,
    string? Horario,
    string? ImageUrl) : IRequest<Result>;
