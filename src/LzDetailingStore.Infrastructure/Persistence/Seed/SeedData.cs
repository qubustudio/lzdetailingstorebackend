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

        await SeedAsync(context);
    }

    public const string SuperAdminRole = "SuperAdmin";
    public const string AdminRole = "Admin";
    public const string OperativoRole = "Operativo";

    public const string RecommendationsModuleSlug = "recomendaciones";
    public const string ManageRecommendationsPermission = "recomendaciones.gestionar";

    public const string ProductsModuleSlug = "productos";
    public const string ProductAvailabilityPermission = "productos.disponibilidad";

    public const string DashboardModuleSlug = "dashboard";
    public const string DashboardSalesPermission = "dashboard.ventas";
    private const string DashboardSalesDescription = "Ver ventas e ingresos en el resumen";

    /// <summary>Default permissions granted to the Operativo role when it is created.</summary>
    public static readonly IReadOnlyList<string> OperativoPermissions =
    [
        "ordenes.ver",
        "ordenes.editar",
        "productos.ver",
        ProductAvailabilityPermission,
        "dashboard.ver"
    ];

    /// <summary>
    /// Seeds the base RBAC data on an empty database and then runs the idempotent ensure steps,
    /// which add permissions and roles introduced after the initial seed. Safe to run on every startup.
    /// </summary>
    public static async Task SeedAsync(DbContext context)
    {
        if (!await context.Set<Role>().AnyAsync())
            await SeedRbacAsync(context);

        // Idempotent: also adds permissions/roles introduced after the initial seed to existing databases.
        await EnsureRecommendationPermissionsAsync(context);
        await EnsureProductAvailabilityPermissionAsync(context);
        await EnsureDashboardSalesPermissionAsync(context);
        await EnsureOperativoRoleAsync(context);
    }

    /// <summary>
    /// Ensures the "Recomendaciones" module and the <c>recomendaciones.gestionar</c> permission exist,
    /// granted to SuperAdmin and Admin (never Cliente).
    /// </summary>
    private static async Task EnsureRecommendationPermissionsAsync(DbContext context)
    {
        var permission = await EnsurePermissionAsync(
            context, RecommendationsModuleSlug, "Recomendaciones",
            "Gestionar", ManageRecommendationsPermission, "Gestionar in Recomendaciones");

        await GrantToRolesAsync(context, permission.Id, SuperAdminRole, AdminRole);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Ensures the <c>productos.disponibilidad</c> permission exists, granted to SuperAdmin and Admin.
    /// </summary>
    private static async Task EnsureProductAvailabilityPermissionAsync(DbContext context)
    {
        var permission = await EnsurePermissionAsync(
            context, ProductsModuleSlug, "Productos",
            "Disponibilidad", ProductAvailabilityPermission, "Disponibilidad in Productos");

        await GrantToRolesAsync(context, permission.Id, SuperAdminRole, AdminRole);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Ensures the <c>dashboard.ventas</c> permission (money figures in the admin dashboard) exists,
    /// granted to SuperAdmin and Admin only. Operativo sees the dashboard without sales or revenue.
    /// </summary>
    private static async Task EnsureDashboardSalesPermissionAsync(DbContext context)
    {
        var permission = await EnsurePermissionAsync(
            context, DashboardModuleSlug, "Dashboard",
            "Ventas", DashboardSalesPermission, DashboardSalesDescription);

        await GrantToRolesAsync(context, permission.Id, SuperAdminRole, AdminRole);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Ensures the Operativo system role exists. Its default permissions are granted only when the role
    /// is created, so later permission changes made to the role are not overwritten on startup.
    /// </summary>
    private static async Task EnsureOperativoRoleAsync(DbContext context)
    {
        if (await context.Set<Role>().AnyAsync(r => r.Nombre == OperativoRole))
            return;

        var role = new Role
        {
            Nombre = OperativoRole,
            Descripcion = "Personal operativo: pedidos y disponibilidad de productos",
            IsSystem = true
        };
        await context.Set<Role>().AddAsync(role);

        var permissionIds = await context.Set<Permission>()
            .Where(p => OperativoPermissions.Contains(p.Slug))
            .Select(p => p.Id)
            .ToListAsync();

        foreach (var permissionId in permissionIds)
            await context.Set<RolePermission>().AddAsync(new RolePermission { RoleId = role.Id, PermissionId = permissionId });

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Returns the permission with the given slug, creating it (and its module, if missing) when needed.
    /// New entities are tracked but not saved.
    /// </summary>
    private static async Task<Permission> EnsurePermissionAsync(
        DbContext context, string moduleSlug, string moduleName,
        string permissionName, string permissionSlug, string description)
    {
        var permission = await context.Set<Permission>()
            .FirstOrDefaultAsync(p => p.Slug == permissionSlug);

        if (permission is not null)
            return permission;

        var module = await context.Set<Module>().FirstOrDefaultAsync(m => m.Slug == moduleSlug);
        if (module is null)
        {
            var maxOrder = await context.Set<Module>().MaxAsync(m => (int?)m.Orden) ?? 0;
            module = new Module { Nombre = moduleName, Slug = moduleSlug, Orden = maxOrder + 1 };
            await context.Set<Module>().AddAsync(module);
        }

        permission = new Permission
        {
            ModuleId = module.Id,
            Nombre = permissionName,
            Slug = permissionSlug,
            Descripcion = description
        };
        await context.Set<Permission>().AddAsync(permission);

        return permission;
    }

    /// <summary>Grants the permission to each named role that does not have it yet (tracked, not saved).</summary>
    private static async Task GrantToRolesAsync(DbContext context, Guid permissionId, params string[] roleNames)
    {
        var roles = await context.Set<Role>()
            .Where(r => roleNames.Contains(r.Nombre))
            .Select(r => new
            {
                r.Id,
                HasPermission = r.RolePermissions.Any(rp => rp.PermissionId == permissionId)
            })
            .ToListAsync();

        foreach (var role in roles.Where(r => !r.HasPermission))
            await context.Set<RolePermission>().AddAsync(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
    }

    private static async Task SeedRbacAsync(DbContext context)
    {

        // Roles (Operativo is created by EnsureOperativoRoleAsync, which also runs on fresh databases)
        var superAdmin = new Role { Nombre = SuperAdminRole, Descripcion = "Full system access", IsSystem = true };
        var admin = new Role { Nombre = AdminRole, Descripcion = "Administrative access", IsSystem = true };
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

        // Extra product permission: toggle variant availability without full edit rights
        var productsModule = modules.Single(m => m.Slug == ProductsModuleSlug);
        permissions.Add(new Permission
        {
            ModuleId = productsModule.Id,
            Nombre = "Disponibilidad",
            Slug = ProductAvailabilityPermission,
            Descripcion = "Disponibilidad in Productos"
        });

        // Extra dashboard permission: see sales/revenue figures (never granted to Operativo)
        var dashboardModule = modules.Single(m => m.Slug == DashboardModuleSlug);
        permissions.Add(new Permission
        {
            ModuleId = dashboardModule.Id,
            Nombre = "Ventas",
            Slug = DashboardSalesPermission,
            Descripcion = DashboardSalesDescription
        });

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
