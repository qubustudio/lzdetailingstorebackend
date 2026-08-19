using LzDetailingStore.Domain.Entities.Business;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Business;

public sealed class BulkImportLogConfiguration : IEntityTypeConfiguration<BulkImportLog>
{
    public void Configure(EntityTypeBuilder<BulkImportLog> builder)
    {
        builder.HasKey(b => b.Id);

        builder.HasIndex(b => b.UserId);
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.CreatedAt);

        builder.Property(b => b.ImportType).HasMaxLength(50).IsRequired();
        builder.Property(b => b.FileName).HasMaxLength(255).IsRequired();
        builder.Property(b => b.FileUrl).HasMaxLength(500);
        builder.Property(b => b.ErrorsDetail).HasColumnType("jsonb");

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(30);
    }
}
