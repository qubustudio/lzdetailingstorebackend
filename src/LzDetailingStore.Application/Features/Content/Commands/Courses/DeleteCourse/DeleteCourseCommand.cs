using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Content.Commands.Courses.DeleteCourse;

public sealed record DeleteCourseCommand(Guid Id) : IRequest<Result>;
