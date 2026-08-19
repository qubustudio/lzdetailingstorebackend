using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Content.Commands.Courses.CreateCourse;
using LzDetailingStore.Application.Features.Content.Commands.Courses.DeleteCourse;
using LzDetailingStore.Application.Features.Content.Commands.Courses.UpdateCourse;
using MediatR;

namespace LzDetailingStore.Api.Endpoints.Content;

public sealed class AdminCourseEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/cursos")
            .WithTags("Admin - Courses")
            .RequireAuthorization("cursos.crear");

        group.MapPost("/", CreateCourse).RequireAuthorization("cursos.crear");
        group.MapPut("/{id:guid}", UpdateCourse).RequireAuthorization("cursos.editar");
        group.MapDelete("/{id:guid}", DeleteCourse).RequireAuthorization("cursos.eliminar");
    }

    private static async Task<IResult> CreateCourse(
        CreateCourseCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToCreatedResponse("/api/v1/cursos");
    }

    private static async Task<IResult> UpdateCourse(
        Guid id, UpdateCourseCommand command, ISender sender, CancellationToken ct)
    {
        if (id != command.Id)
            return Results.BadRequest(new { error = "ID_MISMATCH", message = "El ID no coincide." });

        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> DeleteCourse(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCourseCommand(id), ct);
        return result.ToApiResponse();
    }
}
