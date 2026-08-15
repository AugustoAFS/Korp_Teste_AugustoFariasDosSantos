using Faturamento.Domain.Entities;
using Faturamento.Domain.Interfaces;
using Faturamento.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.InfraStructure.Repositories;

public sealed class ReplicatedProductRepository(FaturamentoDbContext context) : IReplicatedProductRepository
{
    public Task<ReplicatedProduct?> GetById(Guid productId, CancellationToken ct)
        => context.ReplicatedProducts.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId, ct);

    public async Task Upsert(
        Guid productId, string code, string description, bool active, DateTimeOffset updatedAt, CancellationToken ct)
    {
        var afetadas = await context.ReplicatedProducts
            .Where(p => p.ProductId == productId && p.UpdatedAt <= updatedAt)
            .ExecuteUpdateAsync(
                update => update
                    .SetProperty(p => p.Code, code)
                    .SetProperty(p => p.Description, description)
                    .SetProperty(p => p.Active, active)
                    .SetProperty(p => p.UpdatedAt, updatedAt),
                ct);

        if (afetadas > 0) return;

        if (await context.ReplicatedProducts.AnyAsync(p => p.ProductId == productId, ct)) return;

        await context.ReplicatedProducts.AddAsync(
            new ReplicatedProduct(productId, code, description, active, updatedAt), ct);
    }
}
