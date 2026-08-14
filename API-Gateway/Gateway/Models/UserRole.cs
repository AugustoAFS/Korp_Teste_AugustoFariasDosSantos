namespace Gateway.Models;

public sealed class UserRole : AuditableEntity
{
    private UserRole() { }

    public UserRole(long roleId) => RoleId = roleId;

    public long UserId { get; private set; }
    public long RoleId { get; private set; }
    public User User { get; private set; } = null!;
    public Role Role { get; private set; } = null!;
}
