using Gateway.Dtos.Request;
using Gateway.Models;

namespace Gateway.Repositories.Interfaces;

public interface IUserRepository
{
    Task<(IReadOnlyList<User> Items, int Total)> GetPaged(UserFilterRequest filter, CancellationToken ct);

    Task<User?> ByEmail(string email, CancellationToken ct);

    Task<User?> ById(long id, CancellationToken ct);

    Task<bool> EmailInUse(string email, CancellationToken ct);

    Task Add(User user, CancellationToken ct);

    Task SaveChanges(CancellationToken ct);
}
