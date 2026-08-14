using Gateway.Data;
using Gateway.Models;
using Gateway.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Repositories;

public sealed class UserRepository(GatewayDbContext context) : IUserRepository
{
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
