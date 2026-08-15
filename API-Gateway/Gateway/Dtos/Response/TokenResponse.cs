namespace Gateway.Dtos.Response;

public sealed record TokenResponse
{
    public string Token { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
}
