using Gateway.Models;

namespace Gateway.Repositories.Interfaces;

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> ByNames(IEnumerable<string> names, CancellationToken ct);
}
