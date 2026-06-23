using BuildingBlocks.Application.Abstractions;
using Identity.Application.Abstractions;
using Identity.Application.Permissions;
using Identity.Domain.Constants;
using Identity.Domain.Permissions;
using Identity.Domain.Roles;
using Identity.Domain.Users;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Seeding;

public sealed class IdentitySeeder : IIdentitySeeder
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly AdminSeedOptions _adminSeedOptions;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        IdentityDbContext dbContext,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IOptions<AdminSeedOptions> adminSeedOptions,
        IHostEnvironment hostEnvironment,
        ILogger<IdentitySeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _adminSeedOptions = adminSeedOptions.Value;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        await SeedAdminRoleAsync(cancellationToken);
        await SeedSuperAdminRoleAsync(cancellationToken);
        await SeedAdminUserAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        foreach (var code in PermissionCodes.All)
        {
            var exists = await _dbContext.Permissions.AnyAsync(p => p.Code == code, cancellationToken);
            if (exists)
            {
                continue;
            }

            var module = code.Split('.')[0];
            var permission = Permission.Create(code, code, module);
            await _dbContext.Permissions.AddAsync(permission, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminRoleAsync(CancellationToken cancellationToken)
    {
        var adminRole = await _dbContext.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Code == IdentityConstants.Roles.Admin, cancellationToken);

        if (adminRole is null)
        {
            adminRole = Role.Create(
                IdentityConstants.Roles.Admin,
                "Administrator",
                "System administrator role");

            await _dbContext.Roles.AddAsync(adminRole, cancellationToken);
        }

        var allPermissions = await _dbContext.Permissions.ToListAsync(cancellationToken);
        foreach (var permission in allPermissions)
        {
            adminRole.AddPermission(permission);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSuperAdminRoleAsync(CancellationToken cancellationToken)
    {
        var superAdminRole = await _dbContext.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Code == IdentityConstants.Roles.SuperAdmin, cancellationToken);

        if (superAdminRole is null)
        {
            superAdminRole = Role.Create(
                IdentityConstants.Roles.SuperAdmin,
                "Super Administrator",
                "Super administrator role with full access including audit and activity logs");

            await _dbContext.Roles.AddAsync(superAdminRole, cancellationToken);
        }

        var allPermissions = await _dbContext.Permissions.ToListAsync(cancellationToken);
        foreach (var permission in allPermissions)
        {
            superAdminRole.AddPermission(permission);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        if (!_adminSeedOptions.Enabled)
        {
            _logger.LogDebug("Admin seed is disabled; skipping admin user creation.");
            return;
        }

        var userName = _adminSeedOptions.UserName;
        var exists = await _dbContext.Users.AnyAsync(u => u.UserName == userName, cancellationToken);
        if (exists)
        {
            return;
        }

        var password = ResolveAdminPassword();
        var adminRole = await _dbContext.Roles
            .FirstAsync(r => r.Code == IdentityConstants.Roles.Admin, cancellationToken);

        var user = User.Create(
            userName,
            _adminSeedOptions.Email,
            _passwordHasher.HashPassword(password),
            _adminSeedOptions.FullName,
            _dateTimeProvider.UtcNow);

        user.AssignRole(adminRole);

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded admin user {UserName}", userName);
    }

    private string ResolveAdminPassword()
    {
        if (!string.IsNullOrWhiteSpace(_adminSeedOptions.Password))
        {
            return _adminSeedOptions.Password;
        }

        if (_hostEnvironment.IsProduction())
        {
            throw new InvalidOperationException(
                "AdminSeed:Password must be configured in Production environment.");
        }

        _logger.LogWarning("Using development fallback password for admin seed.");
        return "Admin@123456";
    }
}
