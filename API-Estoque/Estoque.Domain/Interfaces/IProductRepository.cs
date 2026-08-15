using Estoque.Domain.Dtos.Request;
using Estoque.Domain.Entities;

namespace Estoque.Domain.Interfaces;

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Items, int Total)> GetPaged(ProductFilterRequest filter, CancellationToken ct);

    Task<Product?> GetById(Guid id, CancellationToken ct);

    Task<bool> CodeInUse(string code, Guid? except, CancellationToken ct);

    Task Add(Product product, CancellationToken ct);

    Task<int?> Debit(Guid productId, int quantity, CancellationToken ct);
}
