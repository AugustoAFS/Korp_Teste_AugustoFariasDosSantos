using Faturamento.Domain.Entities;
using Faturamento.Domain.Interfaces;
using Faturamento.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.InfraStructure.Repositories;

public sealed class ProcessedMessageRepository(FaturamentoDbContext context) : IProcessedMessageRepository
{
    public Task<bool> AlreadyProcessed(Guid messageId, CancellationToken ct)
        => context.ProcessedMessages.AsNoTracking().AnyAsync(m => m.MessageId == messageId, ct);

    public async Task Mark(Guid messageId, string type, CancellationToken ct)
        => await context.ProcessedMessages.AddAsync(new ProcessedMessage(messageId, type), ct);
}
