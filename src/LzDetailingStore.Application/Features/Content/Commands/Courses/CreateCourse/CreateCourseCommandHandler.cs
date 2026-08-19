using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Content;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Content.Commands.Courses.CreateCourse;

public sealed class CreateCourseCommandHandler(
    DbContext dbContext,
    ISlugGenerator slugGenerator,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCourseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCourseCommand request, CancellationToken ct)
    {
        var slug = await slugGenerator.GenerateAsync(request.Title, "courses", ct);

        var course = new Course
        {
            Title = request.Title,
            Slug = slug,
            Description = request.Description,
            Level = request.Level,
            Status = CourseStatus.Draft,
            DurationHours = request.DurationHours,
            ImageUrl = request.ImageUrl,
            Price = request.Price,
            IsActive = true,
            Modules = request.Modules?.Select(m => new CourseModule
            {
                Title = m.Title,
                Description = m.Description,
                Orden = m.Orden,
                Topics = m.Topics.Select(t => new CourseTopic
                {
                    Title = t.Title,
                    Description = t.Description,
                    Orden = t.Orden
                }).ToList()
            }).ToList() ?? []
        };

        await dbContext.Set<Course>().AddAsync(course, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(course.Id);
    }
}
