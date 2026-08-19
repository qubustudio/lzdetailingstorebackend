namespace LzDetailingStore.Application.Features.Content.DTOs;

public sealed record CourseListDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string Level,
    int DurationHours,
    string? ImageUrl,
    decimal Price);

public sealed record CourseDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string Level,
    string Status,
    int DurationHours,
    string? ImageUrl,
    decimal Price,
    bool IsActive,
    IList<CourseModuleDto> Modules);

public sealed record CourseModuleDto(
    Guid Id,
    string Title,
    string? Description,
    int Orden,
    IList<CourseTopicDto> Topics);

public sealed record CourseTopicDto(
    Guid Id,
    string Title,
    string? Description,
    int Orden);
