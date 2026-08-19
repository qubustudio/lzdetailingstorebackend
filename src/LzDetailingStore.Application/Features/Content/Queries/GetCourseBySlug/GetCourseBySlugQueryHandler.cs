using LzDetailingStore.Application.Features.Content.DTOs;
using LzDetailingStore.Domain.Entities.Content;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Content.Queries.GetCourseBySlug;

public sealed class GetCourseBySlugQueryHandler(DbContext dbContext)
    : IRequestHandler<GetCourseBySlugQuery, Result<CourseDto>>
{
    public async Task<Result<CourseDto>> Handle(GetCourseBySlugQuery request, CancellationToken ct)
    {
        var course = await dbContext.Set<Course>()
            .Include(c => c.Modules.OrderBy(m => m.Orden))
                .ThenInclude(m => m.Topics.OrderBy(t => t.Orden))
            .FirstOrDefaultAsync(c => c.Slug == request.Slug
                && c.IsActive && c.Status == CourseStatus.Active, ct);

        if (course is null)
            return Result<CourseDto>.NotFound("Curso no encontrado.");

        var dto = new CourseDto(
            course.Id, course.Title, course.Slug, course.Description,
            course.Level.ToString(), course.Status.ToString(),
            course.DurationHours, course.ImageUrl, course.Price, course.IsActive,
            course.Modules.Select(m => new CourseModuleDto(
                m.Id, m.Title, m.Description, m.Orden,
                m.Topics.Select(t => new CourseTopicDto(
                    t.Id, t.Title, t.Description, t.Orden
                )).ToList()
            )).ToList());

        return dto;
    }
}
