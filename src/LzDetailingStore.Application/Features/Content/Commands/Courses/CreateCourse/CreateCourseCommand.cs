using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using MediatR;

namespace LzDetailingStore.Application.Features.Content.Commands.Courses.CreateCourse;

public sealed record CreateCourseModuleInput(
    string Title,
    string? Description,
    int Orden,
    IList<CreateCourseTopicInput> Topics);

public sealed record CreateCourseTopicInput(string Title, string? Description, int Orden);

public sealed record CreateCourseCommand(
    string Title,
    string? Description,
    CourseLevel Level,
    int DurationHours,
    string? ImageUrl,
    decimal Price,
    IList<CreateCourseModuleInput>? Modules) : IRequest<Result<Guid>>;
