namespace Faturamento.Ai;

public sealed record AiOptions
{
    public const string Section = "Ai";

    public required string ApiKey { get; init; }
    public required string Model { get; init; }
    public required int MaxTokens { get; init; }
    public required int TimeoutSeconds { get; init; }
    public required string BaseUrl { get; init; }

    public bool Enabled => !string.IsNullOrWhiteSpace(ApiKey);
}
