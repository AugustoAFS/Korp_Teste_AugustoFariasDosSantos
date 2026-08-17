using Gateway.Data;
using Gateway.Dtos.Request;
using Gateway.Models;
using Gateway.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Repositories;

public sealed class UserRepository(GatewayDbContext context) : IUserRepository
{
    public async Task<(IReadOnlyList<User> Items, int Total)> GetPaged(
        UserFilterRequest filter, CancellationToken ct)
    {
        var query = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = $"%{filter.Search.Trim()}%";

            query = query.Where(u => EF.Functions.ILike(u.Name, term) || EF.Functions.ILike(u.Email, term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Name)
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<User?> ByEmail(string email, CancellationToken ct)
        => context.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> ById(long id, CancellationToken ct)
        => context.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<bool> EmailInUse(string email, CancellationToken ct)
        => context.Users.AnyAsync(u => u.Email == email, ct);

    public async Task Add(User user, CancellationToken ct)
        => await context.Users.AddAsync(user, ct);

    public Task SaveChanges(CancellationToken ct)
        => context.SaveChangesAsync(ct);
}
