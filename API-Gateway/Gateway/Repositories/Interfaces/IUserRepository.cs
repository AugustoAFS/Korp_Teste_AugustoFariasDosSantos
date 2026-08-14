using Gateway.Models;

namespace Gateway.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> ByEmail(string email, CancellationToken ct);

    Task<User?> ById(long id, CancellationToken ct);

    Task<bool> EmailInUse(string email, CancellationToken ct);

    Task Add(User user, CancellationToken ct);

    Task SaveChanges(CancellationToken ct);
}
