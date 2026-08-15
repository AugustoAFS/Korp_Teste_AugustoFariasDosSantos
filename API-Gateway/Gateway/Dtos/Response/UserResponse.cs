using Gateway.Models;

namespace Gateway.Dtos.Response;

public sealed record UserResponse
{
    public UserResponse(User user, IEnumerable<string> roles)
    {
        Id = user.Id;
        Name = user.Name;
        Email = user.Email;
        Active = user.Active;
        Roles = [.. roles];
    }

    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool Active { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}
