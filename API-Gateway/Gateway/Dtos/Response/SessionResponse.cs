namespace Gateway.Dtos.Response;

public sealed record SessionResponse(string Name, string Email, IReadOnlyList<string> Roles);
