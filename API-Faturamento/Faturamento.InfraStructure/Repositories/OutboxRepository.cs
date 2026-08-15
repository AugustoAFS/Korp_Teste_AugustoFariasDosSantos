using Faturamento.Domain.Dtos.EventListeners;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Interfaces;
using Faturamento.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.InfraStructure.Repositories;

public sealed class OutboxRepository(FaturamentoDbContext context) : IOutboxRepository
{
    public const int MaxAttempts = 10;

    public async Task Add(string type, string payload, CancellationToken ct)
        => await context.OutboxMessages.AddAsync(new OutboxMessage(type, payload), ct);

    public async Task<IReadOnlyList<PendingOutboxMessage>> GetPending(int limit, CancellationToken ct)
        => await context.OutboxMessages
            .AsNoTracking()
            .Where(m => m.PublishedAt == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.CreatedAt)
            .Take(limit)
            .Select(m => new PendingOutboxMessage { Id = m.Id, Type = m.Type, Payload = m.Payload })
            .ToListAsync(ct);

    public Task MarkPublished(Guid id, CancellationToken ct)
    {
        DateTimeOffset? agora = DateTimeOffset.UtcNow;

        return context.OutboxMessages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(update => update.SetProperty(m => m.PublishedAt, agora), ct);
    }

    public Task RegisterFailure(Guid id, string error, CancellationToken ct)
        => context.OutboxMessages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(
                update => update
                    .SetProperty(m => m.Attempts, m => m.Attempts + 1)
                    .SetProperty(m => m.LastError, error),
                ct);
}
