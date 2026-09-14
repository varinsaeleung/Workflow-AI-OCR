using KmOcr.Application.Contracts.Persistence;
using KmOcr.Domain.Auth;
using KmOcr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KmOcr.Infrastructure.Repositories;

/// <summary>
/// Entity Framework repository for users and RBAC permission lookups.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Creates the repository with the application database context.
    /// </summary>
    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adds a user to the current unit of work.
    /// </summary>
    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    /// <summary>
    /// Gets a user by normalized email address.
    /// </summary>
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _context.Users
            .Include(user => user.Roles)
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    /// <summary>
    /// Gets a user by identifier.
    /// </summary>
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _context.Users
            .Include(user => user.Roles)
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets effective permission codes for a user.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var permissions = await (
            from userRole in _context.UserRoles
            join role in _context.Roles on userRole.RoleId equals role.Id
            join rolePermission in _context.RolePermissions on role.Id equals rolePermission.RoleId
            join permission in _context.Permissions on rolePermission.PermissionId equals permission.Id
            where userRole.UserId == userId && role.IsActive
            select permission.Code)
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync(cancellationToken);

        return permissions;
    }
}
