using Estoque.Domain.Dtos.Request;
using Estoque.Domain.Entities;
using Estoque.Domain.Interfaces;
using Estoque.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Estoque.InfraStructure.Repositories;

public sealed class ProductRepository(EstoqueDbContext context) : IProductRepository
{
    public async Task<(IReadOnlyList<Product> Items, int Total)> GetPaged(ProductFilterRequest filter, CancellationToken ct)
    {
        var query = context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = $"%{EscapeWildcards(filter.Search)}%";

            query = query.Where(p =>
                EF.Functions.Like(p.Code, term) || EF.Functions.Like(p.Description, term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.Code)
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<Product?> GetById(Guid id, CancellationToken ct)
        => context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> CodeInUse(string code, Guid? except, CancellationToken ct)
        => context.Products.AnyAsync(p => p.Code == code && (except == null || p.Id != except), ct);

    public async Task Add(Product product, CancellationToken ct)
        => await context.Products.AddAsync(product, ct);

    public async Task<int?> Debit(Guid productId, int quantity, CancellationToken ct)
    {
        DateTimeOffset? now = DateTimeOffset.UtcNow;

        var affected = await context.Products
            .Where(p => p.Id == productId && p.Active && p.Balance >= quantity)
            .ExecuteUpdateAsync(
                update => update
                    .SetProperty(p => p.Balance, p => p.Balance - quantity)
                    .SetProperty(p => p.UpdatedAt, now),
                ct);

        if (affected == 0) return null;

        return await context.Products
            .Where(p => p.Id == productId)
            .Select(p => p.Balance)
            .FirstAsync(ct);
    }

    private static string EscapeWildcards(string term)
        => term
            .Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);
}
