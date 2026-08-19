using LzDetailingStore.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LzDetailingStore.Infrastructure.Persistence.Configurations.Identity;

public sealed class OAuthAccountConfiguration : IEntityTypeConfiguration<OAuthAccount>
{
    public void Configure(EntityTypeBuilder<OAuthAccount> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Provider).HasMaxLength(20).IsRequired();
        builder.Property(o => o.ProviderUserId).HasMaxLength(200).IsRequired();
        builder.HasIndex(o => new { o.Provider, o.ProviderUserId }).IsUnique();
        builder.Property(o => o.Email).HasMaxLength(254);
        builder.HasOne(o => o.User).WithMany(u => u.OAuthAccounts).HasForeignKey(o => o.UserId);
    }
}
