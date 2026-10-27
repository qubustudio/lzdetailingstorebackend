using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.UnitTests.Commerce;

/// <summary>
/// Minimal in-memory DbContext for unit testing ProcessWebhookCommandHandler.
/// Registers only the entities the handler touches, avoiding the PostgreSQL-specific
/// configuration (pgvector extension, xmin concurrency token) that the real
/// AppDbContext applies and that the InMemory provider does not support.
/// </summary>
public sealed class WebhookTestContext(DbContextOptions<WebhookTestContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ignore navigation-only relationships that would pull in unrelated
        // entities/configuration not needed by the webhook handler.
        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Ignore(o => o.ShippingAddress);
            b.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId);
        });

        modelBuilder.Entity<OrderItem>(b => b.HasKey(i => i.Id));

        modelBuilder.Entity<ProductVariant>(b =>
        {
            b.HasKey(v => v.Id);
            b.Ignore(v => v.Product);
        });

        modelBuilder.Entity<Cart>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasMany(c => c.Items).WithOne(i => i.Cart).HasForeignKey(i => i.CartId);
        });

        modelBuilder.Entity<CartItem>(b =>
        {
            b.HasKey(i => i.Id);
            b.Ignore(i => i.Variant);
        });
    }
}
