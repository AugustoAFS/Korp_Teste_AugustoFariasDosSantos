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
}
