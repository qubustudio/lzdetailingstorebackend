using LzDetailingStore.Application.Features.Content.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Content.Queries.GetCourses;

public sealed record GetCoursesQuery : IRequest<Result<List<CourseListDto>>>;
