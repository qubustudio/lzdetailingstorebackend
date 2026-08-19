using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;

namespace LzDetailingStore.Domain.Entities.Content;

public sealed class Course : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CourseLevel Level { get; set; } = CourseLevel.Basico;
    public CourseStatus Status { get; set; } = CourseStatus.Draft;
    public int DurationHours { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CourseModule> Modules { get; set; } = [];
}
