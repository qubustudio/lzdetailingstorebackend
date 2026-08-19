using LzDetailingStore.Application.Features.Content.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Content.Queries.GetCourseBySlug;

public sealed record GetCourseBySlugQuery(string Slug) : IRequest<Result<CourseDto>>;
