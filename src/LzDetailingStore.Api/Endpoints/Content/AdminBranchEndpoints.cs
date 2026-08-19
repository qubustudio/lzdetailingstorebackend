using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Content.Commands.Branches.CreateBranch;
using LzDetailingStore.Application.Features.Content.Commands.Branches.DeleteBranch;
using LzDetailingStore.Application.Features.Content.Commands.Branches.UpdateBranch;
using MediatR;

namespace LzDetailingStore.Api.Endpoints.Content;

public sealed class AdminBranchEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/sucursales")
            .WithTags("Admin - Branches")
            .RequireAuthorization("sucursales.crear");

        group.MapPost("/", CreateBranch).RequireAuthorization("sucursales.crear");
        group.MapPut("/{id:guid}", UpdateBranch).RequireAuthorization("sucursales.editar");
        group.MapDelete("/{id:guid}", DeleteBranch).RequireAuthorization("sucursales.eliminar");
    }

    private static async Task<IResult> CreateBranch(
        CreateBranchCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToCreatedResponse("/api/v1/sucursales");
    }

    private static async Task<IResult> UpdateBranch(
        Guid id, UpdateBranchCommand command, ISender sender, CancellationToken ct)
    {
        if (id != command.Id)
            return Results.BadRequest(new { error = "ID_MISMATCH", message = "El ID no coincide." });

        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> DeleteBranch(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteBranchCommand(id), ct);
        return result.ToApiResponse();
    }
}
