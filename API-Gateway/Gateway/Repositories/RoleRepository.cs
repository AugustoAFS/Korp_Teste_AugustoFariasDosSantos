using Gateway.Data;
using Gateway.Models;
using Gateway.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Repositories;

public sealed class RoleRepository(GatewayDbContext context) : IRoleRepository
{
    public async Task<IReadOnlyList<Role>> ByNames(IEnumerable<string> names, CancellationToken ct)
    {
        var procurados = names.Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        return await context.Roles
            .AsNoTracking()
            .Where(r => r.Active && procurados.Contains(r.Name))
            .ToListAsync(ct);
    }
}
