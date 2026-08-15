namespace Faturamento.Domain.Entities;

public sealed class ReplicatedProduct
{
    private ReplicatedProduct() { }

    public ReplicatedProduct(Guid productId, string code, string description, bool active, DateTimeOffset updatedAt)
    {
        ProductId = productId;
        Code = code;
        Description = description;
        Active = active;
        UpdatedAt = updatedAt;
    }

    public Guid ProductId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool Active { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}
