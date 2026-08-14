namespace Gateway.Models;

public sealed class User : AuditableEntity
{
    private readonly List<UserRole> _roles = [];

    private User() { }

    public User(string name, string email, string passwordHash)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        PasswordChangedAt = DateTimeOffset.UtcNow;
    }

    public long Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool EmailConfirmed { get; private set; }
    public string? PasswordHash { get; private set; }
    public bool Active { get; private set; } = true;
    public int FailedAccessCount { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public DateTimeOffset? PasswordChangedAt { get; private set; }
    public IReadOnlyCollection<UserRole> Roles => _roles;
    public bool Locked => LockedUntil is not null && LockedUntil > DateTimeOffset.UtcNow;
    public void AssignRole(long roleId) => _roles.Add(new UserRole(roleId));

    public void ReplaceRoles(IReadOnlyCollection<long> roleIds)
    {
        _roles.RemoveAll(link => !roleIds.Contains(link.RoleId));

        foreach (var roleId in roleIds.Where(id => _roles.TrueForAll(link => link.RoleId != id)))
            _roles.Add(new UserRole(roleId));
    }

    public void RegisterValidAccess()
    {
        FailedAccessCount = 0;
        LockedUntil = null;
    }

    public void RegisterInvalidAccess(int maxAttempts, TimeSpan lockout)
    {
        FailedAccessCount++;

        if (FailedAccessCount < maxAttempts) return;

        FailedAccessCount = 0;
        LockedUntil = DateTimeOffset.UtcNow.Add(lockout);
    }
}
