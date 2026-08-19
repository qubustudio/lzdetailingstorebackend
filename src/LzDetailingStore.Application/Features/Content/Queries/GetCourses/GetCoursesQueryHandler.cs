using LzDetailingStore.Application.Features.Content.DTOs;
using LzDetailingStore.Domain.Entities.Content;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Content.Queries.GetCourses;

public sealed class GetCoursesQueryHandler(DbContext dbContext)
    : IRequestHandler<GetCoursesQuery, Result<List<CourseListDto>>>
{
    public async Task<Result<List<CourseListDto>>> Handle(GetCoursesQuery request, CancellationToken ct)
    {
        var courses = await dbContext.Set<Course>()
            .Where(c => c.IsActive && c.Status == CourseStatus.Active)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CourseListDto(
                c.Id, c.Title, c.Slug, c.Description,
                c.Level.ToString(), c.DurationHours, c.ImageUrl, c.Price))
            .ToListAsync(ct);

        return courses;
    }
}
