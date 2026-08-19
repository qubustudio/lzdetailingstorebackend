using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Content;

public sealed class CourseTopic : BaseEntity
{
    public Guid ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Orden { get; set; }

    public CourseModule Module { get; set; } = null!;
}
