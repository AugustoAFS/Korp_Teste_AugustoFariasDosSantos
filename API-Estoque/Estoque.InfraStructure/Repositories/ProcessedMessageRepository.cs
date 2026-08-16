using Estoque.Domain.Dtos.EventListeners;
using Estoque.Domain.Entities;
using Estoque.Domain.Interfaces;
using Estoque.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Estoque.InfraStructure.Repositories;

public sealed class ProcessedMessageRepository(EstoqueDbContext context) : IProcessedMessageRepository
{
    public Task<bool> AlreadyProcessed(Guid messageId, CancellationToken ct)
        => context.ProcessedMessages.AsNoTracking().AnyAsync(m => m.MessageId == messageId, ct);

    public async Task Mark(Guid messageId, string type, CancellationToken ct)
        => await context.ProcessedMessages.AddAsync(new ProcessedMessage(messageId, type), ct);

    public Task RecordOutcome(Guid messageId, StoredEvent outcome, CancellationToken ct)
        => context.ProcessedMessages
            .Where(m => m.MessageId == messageId)
            .ExecuteUpdateAsync(
                update => update
                    .SetProperty(m => m.OutcomeType, outcome.Type)
                    .SetProperty(m => m.OutcomePayload, outcome.Payload),
                ct);

    public Task<StoredEvent?> Outcome(Guid messageId, CancellationToken ct)
        => context.ProcessedMessages
            .AsNoTracking()
            .Where(m => m.MessageId == messageId && m.OutcomeType != null && m.OutcomePayload != null)
            .Select(m => new StoredEvent { Type = m.OutcomeType!, Payload = m.OutcomePayload! })
            .FirstOrDefaultAsync(ct);
}
