using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Content.Commands.Branches.CreateBranch;

public sealed record CreateBranchCommand(
    string Nombre,
    string Direccion,
    string? Telefono,
    string? GoogleMapsUrl,
    string? Horario,
    string? ImageUrl) : IRequest<Result<Guid>>;
