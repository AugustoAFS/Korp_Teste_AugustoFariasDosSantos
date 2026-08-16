using Estoque.Domain.Dtos.EventListeners;

namespace Estoque.Domain.Interfaces;

public interface IProcessedMessageRepository
{
    Task<bool> AlreadyProcessed(Guid messageId, CancellationToken ct);

    Task Mark(Guid messageId, string type, CancellationToken ct);

    Task RecordOutcome(Guid messageId, StoredEvent outcome, CancellationToken ct);

    Task<StoredEvent?> Outcome(Guid messageId, CancellationToken ct);
}
