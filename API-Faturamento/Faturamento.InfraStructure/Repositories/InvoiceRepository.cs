using Faturamento.Domain.Dtos.Request;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;
using Faturamento.Domain.Interfaces;
using Faturamento.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.InfraStructure.Repositories;

public sealed class InvoiceRepository(FaturamentoDbContext context) : IInvoiceRepository
{
    public async Task<(IReadOnlyList<Invoice> Items, int Total)> GetPaged(
        InvoiceFilterRequest filter, long? onlyUserId, CancellationToken ct)
    {
        var query = context.Invoices.AsNoTracking();

        if (onlyUserId is not null)
            query = query.Where(i => i.IssuedByUserId == onlyUserId);

        if (filter.Status is not null)
            query = query.Where(i => i.Status == filter.Status);

        var total = await query.CountAsync(ct);

        var items = await query
            .Include(i => i.Items)
            .OrderByDescending(i => i.Number)
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .AsSplitQuery()
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<Invoice?> GetById(long id, CancellationToken ct)
        => context.Invoices
            .Include(i => i.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<Invoice?> GetByProcessing(long id, Guid processingId, CancellationToken ct)
        => context.Invoices
            .Include(i => i.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id && i.ProcessingId == processingId, ct);

    public async Task<IReadOnlyList<Invoice>> Expired(TimeSpan timeout, int limit, CancellationToken ct)
    {
        var limite = DateTimeOffset.UtcNow - timeout;

        return await context.Invoices
            .Where(i => i.ProcessingId != null && i.LastError == null && i.ProcessingStartedAt < limite)
            .OrderBy(i => i.ProcessingStartedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<long> NextNumber(CancellationToken ct)
    {
        var proximo = await context.Database
            .SqlQueryRaw<long>($"SELECT nextval('{FaturamentoDbContext.InvoiceNumberSequence}') AS \"Value\"")
            .ToListAsync(ct);

        return proximo[0];
    }

    public async Task Add(Invoice invoice, CancellationToken ct)
        => await context.Invoices.AddAsync(invoice, ct);

    public async Task<bool> StartPrinting(long id, Guid processingId, CancellationToken ct)
    {
        Guid? processing = processingId;
        DateTimeOffset? agora = DateTimeOffset.UtcNow;

        var afetadas = await context.Invoices
            .Where(i => i.Id == id && i.Status == InvoiceStatus.Open && i.ProcessingId == null)
            .ExecuteUpdateAsync(
                update => update
                    .SetProperty(i => i.ProcessingId, processing)
                    .SetProperty(i => i.ProcessingStartedAt, agora)
                    .SetProperty(i => i.LastError, (string?)null)
                    .SetProperty(i => i.UpdatedAt, agora),
                ct);

        return afetadas == 1;
    }

    public async Task<bool> RestartPrinting(long id, Guid processingId, CancellationToken ct)
    {
        DateTimeOffset? agora = DateTimeOffset.UtcNow;

        var afetadas = await context.Invoices
            .Where(i => i.Id == id
                        && i.Status == InvoiceStatus.Open
                        && i.ProcessingId == processingId
                        && i.LastError != null)
            .ExecuteUpdateAsync(
                update => update
                    .SetProperty(i => i.ProcessingStartedAt, agora)
                    .SetProperty(i => i.LastError, (string?)null)
                    .SetProperty(i => i.UpdatedAt, agora),
                ct);

        return afetadas == 1;
    }
}
