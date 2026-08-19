using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Content.Queries.GetCourseBySlug;
using LzDetailingStore.Application.Features.Content.Queries.GetCourses;
using MediatR;

namespace LzDetailingStore.Api.Endpoints.Content;

public sealed class CourseEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/cursos").WithTags("Courses");

        group.MapGet("/", GetCourses);
        group.MapGet("/{slug}", GetCourseBySlug);
    }

    private static async Task<IResult> GetCourses(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetCoursesQuery(), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> GetCourseBySlug(string slug, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetCourseBySlugQuery(slug), ct);
        return result.ToApiResponse();
    }
}
