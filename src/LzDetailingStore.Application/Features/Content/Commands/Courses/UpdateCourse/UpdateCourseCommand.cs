using LzDetailingStore.Application.Features.Content.Commands.Courses.CreateCourse;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using MediatR;

namespace LzDetailingStore.Application.Features.Content.Commands.Courses.UpdateCourse;

public sealed record UpdateCourseCommand(
    Guid Id,
    string Title,
    string? Description,
    CourseLevel Level,
    CourseStatus Status,
    int DurationHours,
    string? ImageUrl,
    decimal Price,
    IList<CreateCourseModuleInput>? Modules) : IRequest<Result>;
