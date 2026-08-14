using Gateway.Models;

namespace Gateway.Dtos.Response;

public sealed record UserResponse(long Id, string Name, string Email, bool Active, IReadOnlyList<string> Roles)
{
    public UserResponse(User user, IEnumerable<string> roles)
        : this(user.Id, user.Name, user.Email, user.Active, roles.ToArray()) { }
}
