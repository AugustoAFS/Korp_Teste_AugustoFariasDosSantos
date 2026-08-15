using Estoque.Domain.Entities;

namespace Estoque.Domain.Dtos.Response;

public sealed record ProductResponse
{
    public ProductResponse(Product product)
    {
        Id = product.Id;
        Code = product.Code;
        Description = product.Description;
        Balance = product.Balance;
        Active = product.Active;
    }

    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Balance { get; init; }
    public bool Active { get; init; }
}
