namespace Gateway.Dtos.Response;

public sealed record TokenResponse(string Token, int ExpiresIn);
