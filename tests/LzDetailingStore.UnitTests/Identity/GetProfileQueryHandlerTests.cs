using FluentAssertions;
using LzDetailingStore.Application.Features.Identity.Queries.GetProfile;
using LzDetailingStore.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.UnitTests.Identity;

public sealed class GetProfileQueryHandlerTests
{
    private readonly IdentityTestContext _ctx = IdentityTestContext.Create();
    private readonly GetProfileQueryHandler _handler;

    public GetProfileQueryHandlerTests()
    {
        _handler = new GetProfileQueryHandler(_ctx);
    }

    [Fact]
    public async Task UsuarioConVariosRoles_DevuelvePermisosSinDuplicadosYOrdenados()
    {
        var module = new Module { Nombre = "Operacion", Slug = "operacion" };
        var ordenesVer = Permission(module, "ordenes.ver");
        var recomendaciones = Permission(module, "recomendaciones.gestionar");
        var catalogo = Permission(module, "catalogo.editar");

        var operativo = Role("Operativo", recomendaciones, ordenesVer);
        var editor = Role("Editor", ordenesVer, catalogo);

        var user = new User { Nombre = "Ana", Email = "ana@test.com", Telefono = "555" };
        user.UserRoles.Add(new UserRole { User = user, Role = operativo });
        user.UserRoles.Add(new UserRole { User = user, Role = editor });

        _ctx.AddRange(module, user);
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        var result = await _handler.Handle(new GetProfileQuery(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Permisos.Should().Equal("catalogo.editar", "ordenes.ver", "recomendaciones.gestionar");
        result.Value.Roles.Should().BeEquivalentTo("Operativo", "Editor");
        result.Value.Email.Should().Be("ana@test.com");
    }

    [Fact]
    public async Task UsuarioSinRoles_DevuelvePermisosVacios()
    {
        var user = new User { Nombre = "Beto", Email = "beto@test.com" };
        _ctx.Add(user);
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        var result = await _handler.Handle(new GetProfileQuery(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Permisos.Should().NotBeNull().And.BeEmpty();
        result.Value.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task UsuarioInexistente_DevuelveNotFound()
    {
        var result = await _handler.Handle(new GetProfileQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    private static Permission Permission(Module module, string slug) =>
        new() { Module = module, Nombre = slug, Slug = slug };

    private static Role Role(string nombre, params Permission[] permissions)
    {
        var role = new Role { Nombre = nombre };
        foreach (var permission in permissions)
            role.RolePermissions.Add(new RolePermission { Role = role, Permission = permission });
        return role;
    }

    /// <summary>Minimal in-memory DbContext with only the identity entities the profile query touches.</summary>
    private sealed class IdentityTestContext(DbContextOptions<IdentityTestContext> options) : DbContext(options)
    {
        public static IdentityTestContext Create() =>
            new(new DbContextOptionsBuilder<IdentityTestContext>()
                .UseInMemoryDatabase($"identity-tests-{Guid.NewGuid()}")
                .Options);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(u => u.Id);
                b.Ignore(u => u.OAuthAccounts);
                b.Ignore(u => u.RefreshTokens);
            });
            modelBuilder.Entity<Role>(b => b.HasKey(r => r.Id));
            modelBuilder.Entity<Module>(b => b.HasKey(m => m.Id));
            modelBuilder.Entity<Permission>(b => b.HasKey(p => p.Id));
            modelBuilder.Entity<UserRole>(b => b.HasKey(ur => new { ur.UserId, ur.RoleId }));
            modelBuilder.Entity<RolePermission>(b => b.HasKey(rp => new { rp.RoleId, rp.PermissionId }));
        }
    }
}
