using LzDetailingStore.Application.Features.Content.Commands.Courses.CreateCourse;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Content;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Content.Commands.Courses.UpdateCourse;

public sealed class UpdateCourseCommandHandler(
    DbContext dbContext,
    ISlugGenerator slugGenerator,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCourseCommand, Result>
{
    public async Task<Result> Handle(UpdateCourseCommand request, CancellationToken ct)
    {
        var course = await dbContext.Set<Course>()
            .Include(c => c.Modules)
                .ThenInclude(m => m.Topics)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (course is null)
            return Result.NotFound("Curso no encontrado.");

        if (course.Title != request.Title)
            course.Slug = await slugGenerator.GenerateAsync(request.Title, "courses", ct);

        course.Title = request.Title;
        course.Description = request.Description;
        course.Level = request.Level;
        course.Status = request.Status;
        course.DurationHours = request.DurationHours;
        course.ImageUrl = request.ImageUrl;
        course.Price = request.Price;

        // Replace modules and topics (cascade delete handles old ones)
        if (request.Modules is not null)
        {
            dbContext.Set<CourseModule>().RemoveRange(course.Modules);

            course.Modules = request.Modules.Select(m => new CourseModule
            {
                CourseId = course.Id,
                Title = m.Title,
                Description = m.Description,
                Orden = m.Orden,
                Topics = m.Topics.Select(t => new CourseTopic
                {
                    Title = t.Title,
                    Description = t.Description,
                    Orden = t.Orden
                }).ToList()
            }).ToList();
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
