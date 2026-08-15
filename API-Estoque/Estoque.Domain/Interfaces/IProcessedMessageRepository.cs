namespace Estoque.Domain.Interfaces;

public interface IProcessedMessageRepository
{
    Task<bool> AlreadyProcessed(Guid messageId, CancellationToken ct);

    Task Mark(Guid messageId, string type, CancellationToken ct);
}
