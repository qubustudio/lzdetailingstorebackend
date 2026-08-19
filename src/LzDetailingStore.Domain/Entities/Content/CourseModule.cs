using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Content;

public sealed class CourseModule : BaseEntity
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Orden { get; set; }

    public Course Course { get; set; } = null!;
    public ICollection<CourseTopic> Topics { get; set; } = [];
}
