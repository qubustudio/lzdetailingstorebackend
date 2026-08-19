using LzDetailingStore.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Content;

public sealed class CourseTopicConfiguration : IEntityTypeConfiguration<CourseTopic>
{
    public void Configure(EntityTypeBuilder<CourseTopic> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(1000);
    }
}
