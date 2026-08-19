using LzDetailingStore.Domain.Entities.Content;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Content.Commands.Courses.DeleteCourse;

public sealed class DeleteCourseCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCourseCommand, Result>
{
    public async Task<Result> Handle(DeleteCourseCommand request, CancellationToken ct)
    {
        var course = await dbContext.Set<Course>()
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (course is null)
            return Result.NotFound("Curso no encontrado.");

        course.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
