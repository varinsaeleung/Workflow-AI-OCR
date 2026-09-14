using KmOcr.Application.Auth;
using KmOcr.Application.Contracts.Security;
using KmOcr.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KmOcr.Infrastructure.Persistence;

/// <summary>
/// Seeds development-only identity data so Docker Compose can be used immediately.
/// </summary>
public static class DevelopmentDataSeeder
{
    private const string AdminRoleCode = "platform-admin";

    /// <summary>
    /// Creates required permissions, the platform admin role, and the development admin user.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var permissions = await SeedPermissionsAsync(context, cancellationToken);
        var adminRole = await SeedAdminRoleAsync(context, permissions, cancellationToken);
        await SeedAdminUserAsync(context, passwordHasher, configuration, adminRole, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Ensures all permission definitions exist.
    /// </summary>
    private static async Task<IReadOnlyList<Permission>> SeedPermissionsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var existingPermissions = await context.Permissions.ToListAsync(cancellationToken);
        var permissionsByCode = existingPermissions.ToDictionary(permission => permission.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var code in PermissionCodes.All)
        {
            if (permissionsByCode.ContainsKey(code))
            {
                continue;
            }

            var permission = Permission.Create(code, PermissionCodes.GetName(code), PermissionCodes.GetModule(code));
            await context.Permissions.AddAsync(permission, cancellationToken);
            permissionsByCode.Add(code, permission);
        }

        return permissionsByCode.Values.ToList();
    }

    /// <summary>
    /// Ensures the platform admin role exists and grants every permission.
    /// </summary>
    private static async Task<Role> SeedAdminRoleAsync(ApplicationDbContext context, IReadOnlyList<Permission> permissions, CancellationToken cancellationToken)
    {
        var adminRole = await context.Roles
            .Include(role => role.Permissions)
            .SingleOrDefaultAsync(role => role.Code == AdminRoleCode, cancellationToken);

        if (adminRole is null)
        {
            adminRole = Role.Create(AdminRoleCode, "Platform Administrator", isSystemRole: true);
            await context.Roles.AddAsync(adminRole, cancellationToken);
        }

        foreach (var permission in permissions)
        {
            adminRole.GrantPermission(permission.Id);
        }

        return adminRole;
    }

    /// <summary>
    /// Ensures the configured development admin user exists and has the platform admin role.
    /// </summary>
    private static async Task SeedAdminUserAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        Role adminRole,
        CancellationToken cancellationToken)
    {
        var email = (configuration["SeedAdmin:Email"] ?? "admin@km.local").Trim().ToLowerInvariant();
        var displayName = configuration["SeedAdmin:DisplayName"] ?? "System Administrator";
        var password = configuration["SeedAdmin:Password"] ?? "ChangeMe!2026";
        var adminUser = await context.Users
            .Include(user => user.Roles)
            .SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

        if (adminUser is null)
        {
            adminUser = User.Create(email, displayName, passwordHasher.HashPassword(password));
            await context.Users.AddAsync(adminUser, cancellationToken);
        }

        adminUser.AssignRole(adminRole.Id);
    }
}
