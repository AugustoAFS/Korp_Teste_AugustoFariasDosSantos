using Estoque.Domain.Dtos.EventListeners;

namespace Estoque.Domain.Interfaces;

public interface IOutboxRepository
{
    Task Add(string type, string payload, CancellationToken ct);

    Task<IReadOnlyList<PendingOutboxMessage>> GetPending(int limit, CancellationToken ct);

    Task MarkPublished(Guid id, CancellationToken ct);

    Task RegisterFailure(Guid id, string error, CancellationToken ct);
}
