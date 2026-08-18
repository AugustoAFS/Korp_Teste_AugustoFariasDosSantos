using Faturamento.Domain.Entities;

namespace Faturamento.Domain.Interfaces;

public interface IReplicatedProductRepository
{
    Task<ReplicatedProduct?> GetById(Guid productId, CancellationToken ct);

    Task<IReadOnlyList<ReplicatedProduct>> ActiveCatalog(int limit, CancellationToken ct);

    Task Upsert(Guid productId, string code, string description, bool active, DateTimeOffset updatedAt, CancellationToken ct);
}
