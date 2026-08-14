namespace Gateway.Models;

public sealed class Role : AuditableEntity
{
    private Role() { }

    public Role(long id, string name, string? description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public long Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool Active { get; private set; } = true;
}
