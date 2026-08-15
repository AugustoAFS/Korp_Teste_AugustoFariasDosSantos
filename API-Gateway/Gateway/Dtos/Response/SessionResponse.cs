namespace Gateway.Dtos.Response;

public sealed record SessionResponse
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = [];
}
