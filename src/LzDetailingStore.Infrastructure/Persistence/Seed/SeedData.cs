using LzDetailingStore.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LzDetailingStore.Infrastructure.Persistence.Seed;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();

        if (await context.Set<Role>().AnyAsync()) return; // Already seeded

        // Roles
        var superAdmin = new Role { Nombre = "SuperAdmin", Descripcion = "Full system access", IsSystem = true };
        var admin = new Role { Nombre = "Admin", Descripcion = "Administrative access", IsSystem = true };
        var cliente = new Role { Nombre = "Cliente", Descripcion = "Regular customer", IsSystem = true };

        await context.Set<Role>().AddRangeAsync(superAdmin, admin, cliente);

        // Modules
        var moduleNames = new[] { "Productos", "Categorias", "Marcas", "Cursos", "Sucursales", "Ordenes", "Usuarios", "Roles", "Configuracion", "Dashboard" };
        var modules = new List<Module>();
        for (var i = 0; i < moduleNames.Length; i++)
        {
            var module = new Module
            {
                Nombre = moduleNames[i],
                Slug = moduleNames[i].ToLowerInvariant(),
                Orden = i + 1
            };
            modules.Add(module);
        }
        await context.Set<Module>().AddRangeAsync(modules);

        // Permissions (4 per module)
        var actions = new[] { ("Ver", "ver"), ("Crear", "crear"), ("Editar", "editar"), ("Eliminar", "eliminar") };
        var permissions = new List<Permission>();

        foreach (var module in modules)
        {
            foreach (var (actionName, actionSlug) in actions)
            {
                permissions.Add(new Permission
                {
                    ModuleId = module.Id,
                    Nombre = actionName,
                    Slug = $"{module.Slug}.{actionSlug}",
                    Descripcion = $"{actionName} in {module.Nombre}"
                });
            }
        }
        await context.Set<Permission>().AddRangeAsync(permissions);

        // SuperAdmin gets ALL permissions
        foreach (var p in permissions)
            await context.Set<RolePermission>().AddAsync(new RolePermission { RoleId = superAdmin.Id, PermissionId = p.Id });

        // Admin gets all EXCEPT usuarios.*, roles.*
        var adminPermissions = permissions
            .Where(p => !p.Slug.StartsWith("usuarios.") && !p.Slug.StartsWith("roles."));
        foreach (var p in adminPermissions)
            await context.Set<RolePermission>().AddAsync(new RolePermission { RoleId = admin.Id, PermissionId = p.Id });

        // Cliente gets no admin permissions

        await context.SaveChangesAsync();
    }
}
