namespace Estoque.Domain.Entities;

public sealed class Product : AuditableEntity
{
    private Product() { }

    public Product(string code, string description, int initialBalance)
    {
        Id = Guid.CreateVersion7();
        Code = code;
        Description = description;
        Balance = initialBalance;
        Active = true;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Balance { get; private set; }
    public bool Active { get; private set; }

    public void Update(string code, string description, bool active)
    {
        Code = code;
        Description = description;
        Active = active;
    }

    public void Delete()
    {
        Active = false;
        DeletedAt = DateTimeOffset.UtcNow;
    }
}
