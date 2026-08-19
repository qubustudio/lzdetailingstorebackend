using LzDetailingStore.Domain.Entities.Business;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Business;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Rate).HasPrecision(18, 4);
        builder.Property(e => e.Source).HasMaxLength(50);
        builder.HasIndex(e => e.FetchedAt);
    }
}
