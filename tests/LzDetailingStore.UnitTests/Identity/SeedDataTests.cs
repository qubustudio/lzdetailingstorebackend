using FluentAssertions;
using LzDetailingStore.Domain.Entities.Identity;
using LzDetailingStore.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.UnitTests.Identity;

public sealed class SeedDataTests
{
    private readonly RbacTestContext _ctx = RbacTestContext.Create();

    private static readonly string[] ExpectedOperativoPermissions =
        ["ordenes.ver", "ordenes.editar", "productos.ver", "productos.disponibilidad", "dashboard.ver"];

    [Fact]
    public async Task BaseNueva_CreaPermisoDisponibilidadYRolOperativo()
    {
        await SeedData.SeedAsync(_ctx);

        (await PermissionsOfAsync("Operativo")).Should().BeEquivalentTo(ExpectedOperativoPermissions);
        (await PermissionsOfAsync("SuperAdmin")).Should().Contain("productos.disponibilidad");
        (await PermissionsOfAsync("Admin")).Should().Contain("productos.disponibilidad");
        (await PermissionsOfAsync("Cliente")).Should().BeEmpty();

        var operativo = await _ctx.Set<Role>().SingleAsync(r => r.Nombre == "Operativo");
        operativo.IsSystem.Should().BeTrue();
        operativo.Descripcion.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Operativo_NoTieneProductosEditarNiPermisosAdministrativos()
    {
        await SeedData.SeedAsync(_ctx);

        (await PermissionsOfAsync("Operativo")).Should()
            .NotContain(["productos.editar", "productos.crear", "productos.eliminar", "usuarios.ver", "roles.ver"]);
    }

    [Fact]
    public async Task EjecutarVariasVeces_NoDuplicaRolesPermisosNiAsignaciones()
    {
        await SeedData.SeedAsync(_ctx);
        var roles = await _ctx.Set<Role>().CountAsync();
        var permissions = await _ctx.Set<Permission>().CountAsync();
        var assignments = await _ctx.Set<RolePermission>().CountAsync();

        await SeedData.SeedAsync(_ctx);
        await SeedData.SeedAsync(_ctx);

        (await _ctx.Set<Role>().CountAsync()).Should().Be(roles);
        (await _ctx.Set<Permission>().CountAsync()).Should().Be(permissions);
        (await _ctx.Set<RolePermission>().CountAsync()).Should().Be(assignments);
        (await _ctx.Set<Role>().CountAsync(r => r.Nombre == "Operativo")).Should().Be(1);
    }

    [Fact]
    public async Task BaseExistenteSinCambiosNuevos_AgregaPermisoYRolOperativo()
    {
        // Simulates a database seeded before productos.disponibilidad and Operativo existed.
        await SeedData.SeedAsync(_ctx);
        await RemoveOperativoAndAvailabilityPermissionAsync();

        await SeedData.SeedAsync(_ctx);

        (await PermissionsOfAsync("Operativo")).Should().BeEquivalentTo(ExpectedOperativoPermissions);
        (await PermissionsOfAsync("SuperAdmin")).Should().Contain("productos.disponibilidad");
        (await PermissionsOfAsync("Admin")).Should().Contain("productos.disponibilidad");
        (await _ctx.Set<Permission>().CountAsync(p => p.Slug == "productos.disponibilidad")).Should().Be(1);
    }

    private async Task RemoveOperativoAndAvailabilityPermissionAsync()
    {
        var operativo = await _ctx.Set<Role>().SingleAsync(r => r.Nombre == "Operativo");
        var availability = await _ctx.Set<Permission>().SingleAsync(p => p.Slug == "productos.disponibilidad");

        _ctx.Set<RolePermission>().RemoveRange(
            await _ctx.Set<RolePermission>()
                .Where(rp => rp.RoleId == operativo.Id || rp.PermissionId == availability.Id)
                .ToListAsync());
        _ctx.Set<Role>().Remove(operativo);
        _ctx.Set<Permission>().Remove(availability);
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();
    }

    private async Task<List<string>> PermissionsOfAsync(string roleName) =>
        await _ctx.Set<RolePermission>()
            .Where(rp => rp.Role.Nombre == roleName)
            .Select(rp => rp.Permission.Slug)
            .ToListAsync();
}

/// <summary>
/// Minimal in-memory DbContext with only the RBAC entities, avoiding the PostgreSQL-specific
/// configuration of the real AppDbContext that the InMemory provider does not support.
/// </summary>
public sealed class RbacTestContext(DbContextOptions<RbacTestContext> options) : DbContext(options)
{
    public static RbacTestContext Create()
    {
        var options = new DbContextOptionsBuilder<RbacTestContext>()
            .UseInMemoryDatabase($"rbac-tests-{Guid.NewGuid()}")
            .Options;
        return new RbacTestContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(b =>
        {
            b.HasKey(r => r.Id);
            b.Ignore(r => r.UserRoles);
        });

        modelBuilder.Entity<Module>(b => b.HasKey(m => m.Id));

        modelBuilder.Entity<Permission>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasOne(p => p.Module).WithMany(m => m.Permissions).HasForeignKey(p => p.ModuleId);
        });

        modelBuilder.Entity<RolePermission>(b =>
        {
            b.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            b.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions).HasForeignKey(rp => rp.RoleId);
            b.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId);
        });
    }
}
